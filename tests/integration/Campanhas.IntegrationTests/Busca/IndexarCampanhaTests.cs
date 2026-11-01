using Campanhas.Api.Consumidores;
using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.IntegrationTests.Suporte;
using ConexaoSolidaria.Contracts.V1;
using Elastic.Clients.Elasticsearch;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes do consumidor de indexação com um Elasticsearch real e o Test Harness do MassTransit: o evento publicado vira
/// documento pesquisável, eventos fora de ordem não desfazem dados novos e campanha cancelada sai da busca.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class IndexarCampanhaTests(ElasticsearchFixture elasticsearch)
{
    /// <summary>
    /// Dado um evento de campanha ativa, quando publicado, então a campanha passa a ser achada pela busca.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Publicar_CampanhaAtiva_FicaPesquisavel()
    {
        var evento = Evento("Páscoa Feliz", "Ativa", versao: 1);
        await using var provedor = await IniciarAsync();
        var harness = provedor.GetRequiredService<ITestHarness>();

        await harness.Bus.Publish(evento, TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness);

        var pagina = await provedor.GetRequiredService<IBuscaCampanhas>().BuscarAsync("pascoa", 1, 10, TestContext.Current.CancellationToken);
        pagina.Itens.Select(i => i.Id).ShouldBe([evento.CampanhaId]);
    }

    /// <summary>
    /// Dado um evento de cancelamento depois do de criação, quando ambos são consumidos, então a campanha deixa de aparecer.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Publicar_CampanhaCancelada_SaiDaBusca()
    {
        var id = Guid.NewGuid();
        await using var provedor = await IniciarAsync();
        var harness = provedor.GetRequiredService<ITestHarness>();

        await harness.Bus.Publish(Evento("Inverno Quente", "Ativa", versao: 1, id), TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 1);
        await harness.Bus.Publish(Evento("Inverno Quente", "Cancelada", versao: 2, id), TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 2);

        var pagina = await provedor.GetRequiredService<IBuscaCampanhas>().BuscarAsync("inverno", 1, 10, TestContext.Current.CancellationToken);
        pagina.Itens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um evento antigo que chega depois de um mais novo, quando consumido, então o documento novo permanece.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Publicar_EventoForaDeOrdem_NaoSobrescreveVersaoNova()
    {
        var id = Guid.NewGuid();
        await using var provedor = await IniciarAsync();
        var harness = provedor.GetRequiredService<ITestHarness>();

        await harness.Bus.Publish(Evento("Horta Nova", "Ativa", versao: 10, id), TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 1);
        await harness.Bus.Publish(Evento("Horta Antiga", "Ativa", versao: 9, id), TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 2);

        var busca = provedor.GetRequiredService<IBuscaCampanhas>();
        (await busca.BuscarAsync("horta", 1, 10, TestContext.Current.CancellationToken)).Itens.Select(i => i.Titulo).ShouldBe(["Horta Nova"]);
    }

    /// <summary>
    /// Espera o consumidor concluir o número de mensagens esperado e deixa o Elasticsearch tornar o documento pesquisável.
    /// </summary>
    /// <param name="harness">Test Harness do bus.</param>
    /// <param name="consumidas">Quantidade de mensagens já consumidas esperada.</param>
    /// <returns>Tarefa que termina com o índice atualizado.</returns>
    private async Task EsperarConsumoAsync(ITestHarness harness, int consumidas = 1)
    {
        var ct = TestContext.Current.CancellationToken;
        var limite = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < limite && await harness.Consumed.SelectAsync<CampanhaAlteradaEvent>(ct).Count() < consumidas)
        {
            await Task.Delay(50, ct);
        }

        await elasticsearch.Cliente.Indices.RefreshAsync(MapeamentoIndice.NomeAlias, ct);
    }

    /// <summary>
    /// Zera o índice e inicia um bus em memória com o consumidor de produção ligado ao Elasticsearch do container.
    /// </summary>
    /// <returns>O provedor de serviços com o harness já iniciado.</returns>
    private async Task<ServiceProvider> IniciarAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await elasticsearch.Cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        var provedor = new ServiceCollection()
            .AddLogging()
            .AddSingleton(elasticsearch.CriarCliente())
            .AddSingleton<IIndiceCampanhas, IndiceCampanhasElasticsearch>()
            .AddSingleton<IBuscaCampanhas, BuscaCampanhasElasticsearch>()
            .AddSingleton<IMetricasBusca, MetricasBuscaPrometheus>()
            .AddSingleton(new PoliticaDeRetentativa(2, [TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)]))
            .AddMassTransitTestHarness(registro =>
            {
                registro.AddConsumer<IndexarCampanhaConsumer, IndexarCampanhaConsumerDefinition>();
                registro.UsingInMemory((contexto, bus) =>
                {
                    bus.UseDelayedMessageScheduler();
                    bus.ConfigureEndpoints(contexto);
                });
            })
            .BuildServiceProvider(true);
        await provedor.GetRequiredService<ITestHarness>().Start();
        return provedor;
    }

    /// <summary>
    /// Cria um evento de alteração de campanha.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="status">Situação.</param>
    /// <param name="versao">Versão do evento.</param>
    /// <param name="id">Identificador; sorteado quando não informado.</param>
    /// <returns>O evento.</returns>
    private static CampanhaAlteradaEvent Evento(string titulo, string status, long versao, Guid? id = null)
    {
        return new CampanhaAlteradaEvent(id ?? Guid.NewGuid(), titulo, "Ajude quem precisa", status, 2000m, new DateOnly(2026, 12, 24), versao);
    }
}
