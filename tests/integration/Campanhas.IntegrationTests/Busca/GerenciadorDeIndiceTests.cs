using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Time.Testing;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes do gerenciador de índices num Elasticsearch real: criação, carga em massa, publicação com troca atômica do alias,
/// descarte e falhas.
/// </summary>
/// <param name="elasticsearch">Elasticsearch compartilhado pela coleção.</param>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class GerenciadorDeIndiceTests(ElasticsearchFixture elasticsearch)
{
    /// <summary>
    /// Relógio fixo, que dá nome previsível ao índice novo.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 10, 14, 10, 5, 123, TimeSpan.Zero));

    /// <summary>
    /// Dado um índice com o alias, quando um índice novo é carregado e publicado, então o alias passa para ele, os documentos
    /// ficam pesquisáveis e o índice antigo é apagado.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Publicar_IndiceNovoCarregado_TrocaOAliasEApagaOAntigo()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();
        await cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        await MapeamentoIndice.GarantirAliasAsync(cliente, ct);
        var gerenciador = new GerenciadorDeIndiceElasticsearch(cliente, _relogio);

        var novo = await gerenciador.CriarIndiceNovoAsync(ct);
        await gerenciador.GravarLoteAsync(novo, [Documento("Feira de livros"), Documento("Feira de brinquedos")], ct);
        await gerenciador.GravarLoteAsync(novo, [], ct);
        await gerenciador.PublicarIndiceAsync(novo, ct);

        novo.ShouldBe("campanhas-v20261110141005123");
        (await cliente.Indices.ExistsAsync(MapeamentoIndice.NomeIndiceInicial, ct)).Exists.ShouldBeFalse();
        var alias = await cliente.Indices.GetAliasAsync(g => g.Name(MapeamentoIndice.NomeAlias), ct);
        alias.Aliases!.Keys.Select(k => k.ToString()).ShouldBe([novo]);
        var pagina = await new BuscaCampanhasElasticsearch(cliente).BuscarAsync("feira", 1, 10, ct);
        pagina.Total.ShouldBe(2);
    }

    /// <summary>
    /// Dado um Elasticsearch sem índice nem alias, quando um índice novo é publicado, então ele passa a servir o alias.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Publicar_SemAliasAnterior_CriaOAlias()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();
        await cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        var gerenciador = new GerenciadorDeIndiceElasticsearch(cliente, _relogio);

        var novo = await gerenciador.CriarIndiceNovoAsync(ct);
        await gerenciador.PublicarIndiceAsync(novo, ct);

        (await cliente.Indices.ExistsAliasAsync(MapeamentoIndice.NomeAlias, ct)).Exists.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um índice novo não publicado, quando é descartado, então some; descartar de novo (já inexistente) não falha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Descartar_IndiceNaoPublicado_ApagaESemFalhaSeNaoExiste()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();
        var gerenciador = new GerenciadorDeIndiceElasticsearch(cliente, _relogio);
        var novo = await gerenciador.CriarIndiceNovoAsync(ct);

        await gerenciador.DescartarIndiceAsync(novo, ct);
        await gerenciador.DescartarIndiceAsync(novo, ct);

        (await cliente.Indices.ExistsAsync(novo, ct)).Exists.ShouldBeFalse();
    }

    /// <summary>
    /// Dado um documento que o Elasticsearch recusa (versão negativa), quando o lote é gravado, então a falha é sinalizada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task GravarLote_DocumentoRecusado_LancaOperacaoInvalida()
    {
        var ct = TestContext.Current.CancellationToken;
        var gerenciador = new GerenciadorDeIndiceElasticsearch(elasticsearch.CriarCliente(), _relogio);
        var novo = await gerenciador.CriarIndiceNovoAsync(ct);

        await Should.ThrowAsync<InvalidOperationException>(() => gerenciador.GravarLoteAsync(novo, [Documento("Inválida") with { Versao = -1 }], ct));

        await gerenciador.DescartarIndiceAsync(novo, ct);
    }

    /// <summary>
    /// Dado um Elasticsearch inalcançável, quando qualquer operação é tentada, então a falha vira <see cref="BuscaIndisponivelException"/>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Operacoes_ElasticsearchInalcancavel_LancamIndisponivel()
    {
        var ct = TestContext.Current.CancellationToken;
        var gerenciador = new GerenciadorDeIndiceElasticsearch(new ElasticsearchClient(new Uri("http://127.0.0.1:1")), _relogio);

        await Should.ThrowAsync<BuscaIndisponivelException>(() => gerenciador.CriarIndiceNovoAsync(ct));
        await Should.ThrowAsync<BuscaIndisponivelException>(() => gerenciador.GravarLoteAsync("campanhas-v1", [Documento("X")], ct));
        await Should.ThrowAsync<BuscaIndisponivelException>(() => gerenciador.PublicarIndiceAsync("campanhas-v1", ct));
        await Should.ThrowAsync<BuscaIndisponivelException>(() => gerenciador.DescartarIndiceAsync("campanhas-v1", ct));
    }

    /// <summary>
    /// Dados argumentos inválidos, quando as operações são chamadas, então são recusadas.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Operacoes_ArgumentosInvalidos_LancamExcecao()
    {
        var ct = TestContext.Current.CancellationToken;
        var gerenciador = new GerenciadorDeIndiceElasticsearch(elasticsearch.Cliente, _relogio);

        await Should.ThrowAsync<ArgumentException>(() => gerenciador.GravarLoteAsync(" ", [], ct));
        await Should.ThrowAsync<ArgumentNullException>(() => gerenciador.GravarLoteAsync("campanhas-v1", null!, ct));
        await Should.ThrowAsync<ArgumentException>(() => gerenciador.PublicarIndiceAsync(" ", ct));
        await Should.ThrowAsync<ArgumentException>(() => gerenciador.DescartarIndiceAsync(" ", ct));
    }

    /// <summary>
    /// Cria um documento de campanha ativa.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <returns>O documento.</returns>
    private static CampanhaIndexada Documento(string titulo)
    {
        return new CampanhaIndexada(Guid.NewGuid(), titulo, "Descrição", "Ativa", 100m, new DateOnly(2026, 12, 24), 1);
    }
}
