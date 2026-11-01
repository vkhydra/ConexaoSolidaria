using Campanhas.Api.Consumidores;
using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Mensageria;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.UnitTests.Busca;

/// <summary>
/// Testes do <see cref="IndexarCampanhaConsumer"/> e da sua definição com o Test Harness do MassTransit (transporte em
/// memória): indexação, retentativa em falha transitória e falha imediata em recusa do Elasticsearch.
/// </summary>
[Trait("Category", "Unit")]
public sealed class IndexarCampanhaConsumerTests
{
    /// <summary>
    /// Evento usado nos cenários.
    /// </summary>
    private static readonly CampanhaAlteradaEvent _evento = new(Guid.NewGuid(), "Natal Solidário", "Presentes", "Ativa", 5000m, new DateOnly(2026, 12, 24), 42);

    /// <summary>
    /// Índice de busca falso.
    /// </summary>
    private readonly IIndiceCampanhas _indice = Substitute.For<IIndiceCampanhas>();

    /// <summary>
    /// Dado um evento de alteração, quando consumido, então o documento é gravado no índice com a versão do evento.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Consumir_Evento_GravaDocumentoNoIndice()
    {
        _indice.GravarAsync(Arg.Any<CampanhaIndexada>(), Arg.Any<CancellationToken>()).Returns(ResultadoIndexacao.Indexada);
        await using var provedor = CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(_evento, TestContext.Current.CancellationToken);

        await EsperarChamadasAsync(1);
        await _indice.Received(1).GravarAsync(CampanhaIndexada.De(_evento), Arg.Any<CancellationToken>());
        (await harness.Published.Any<Fault<CampanhaAlteradaEvent>>(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    /// <summary>
    /// Dado um Elasticsearch que falha três vezes (tentativa e duas imediatas), quando o evento é consumido, então a reentrega
    /// atrasada tem sucesso na quarta chamada e nenhuma falha é publicada.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Consumir_FalhaTransitoria_ReentregaEConclui()
    {
        var chamadas = 0;
        _indice.GravarAsync(Arg.Any<CampanhaIndexada>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++chamadas <= 3 ? throw new BuscaIndisponivelException() : Task.FromResult(ResultadoIndexacao.Indexada));
        await using var provedor = CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(_evento, TestContext.Current.CancellationToken);

        await EsperarChamadasAsync(4);
        (await harness.Published.Any<Fault<CampanhaAlteradaEvent>>(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    /// <summary>
    /// Dada uma recusa do Elasticsearch (4xx), quando o evento é consumido, então não há nova tentativa e a falha é publicada.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Consumir_RecusaDoElasticsearch_FalhaSemRetentativa()
    {
        _indice.GravarAsync(Arg.Any<CampanhaIndexada>(), Arg.Any<CancellationToken>())
            .Returns<ResultadoIndexacao>(_ => throw new InvalidOperationException("recusado"));
        await using var provedor = CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(_evento, TestContext.Current.CancellationToken);

        (await harness.Published.Any<Fault<CampanhaAlteradaEvent>>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _indice.Received(1).GravarAsync(Arg.Any<CampanhaIndexada>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada a definição de produção, quando consultada, então a fila é <c>indexar-campanha</c>.
    /// </summary>
    [Fact]
    public void Definicao_NomeDaFila_EIndexarCampanha()
    {
        IndexarCampanhaConsumerDefinition.NomeDaFila.ShouldBe("indexar-campanha");
    }

    /// <summary>
    /// Espera, por até 15 segundos, o índice falso receber o número de chamadas esperado.
    /// </summary>
    /// <param name="esperadas">Quantidade de chamadas a <c>GravarAsync</c>.</param>
    /// <returns>Tarefa que termina quando as chamadas chegaram ou o prazo acabou.</returns>
    private async Task EsperarChamadasAsync(int esperadas)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (_indice.ReceivedCalls().Count() < esperadas && DateTime.UtcNow < limite)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        _indice.ReceivedCalls().Count().ShouldBe(esperadas);
    }

    /// <summary>
    /// Monta o contêiner com o consumidor de produção, o Test Harness e retentativas curtas.
    /// </summary>
    /// <returns>O provedor de serviços.</returns>
    private ServiceProvider CriarProvedor()
    {
        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(_indice)
            .AddSingleton(new PoliticaDeRetentativa(2, [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)]))
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
    }
}
