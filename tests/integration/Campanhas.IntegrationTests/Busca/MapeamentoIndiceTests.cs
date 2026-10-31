using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Elastic.Clients.Elasticsearch;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes da criação do índice de campanhas num Elasticsearch real: alias, analisador em português e idempotência.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MapeamentoIndiceTests(ElasticsearchFixture elasticsearch) : IClassFixture<ElasticsearchFixture>
{
    /// <summary>
    /// Dado um Elasticsearch vazio, quando o alias é garantido duas vezes, então o índice inicial nasce uma vez com o alias.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task GarantirAlias_ElasticsearchVazio_CriaIndiceInicialComAlias()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();
        await cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);

        await MapeamentoIndice.GarantirAliasAsync(cliente, ct);
        await MapeamentoIndice.GarantirAliasAsync(cliente, ct);

        var alias = await cliente.Indices.GetAliasAsync(g => g.Name(MapeamentoIndice.NomeAlias), ct);
        alias.IsValidResponse.ShouldBeTrue();
        alias.Aliases!.Keys.Select(k => k.ToString()).ShouldBe([MapeamentoIndice.NomeIndiceInicial]);
    }

    /// <summary>
    /// Dado o índice criado, quando o texto "Páscoa" é analisado, então vira o mesmo termo que "pascoa" (acento e caixa ignorados).
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Analisador_PtBr_IgnoraAcentoECaixa()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();
        await cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        await MapeamentoIndice.GarantirAliasAsync(cliente, ct);

        var comAcento = await cliente.Indices.AnalyzeAsync(a => a.Index(MapeamentoIndice.NomeAlias).Analyzer("pt_br").Text("Páscoa"), ct);
        var semAcento = await cliente.Indices.AnalyzeAsync(a => a.Index(MapeamentoIndice.NomeAlias).Analyzer("pt_br").Text("PASCOA"), ct);

        comAcento.Tokens!.Select(t => t.Token).ShouldBe(semAcento.Tokens!.Select(t => t.Token));
    }

    /// <summary>
    /// Dado um índice que já existe, quando é criado de novo, então nada muda e nenhuma exceção é lançada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task CriarIndice_JaExiste_NaoFalha()
    {
        var ct = TestContext.Current.CancellationToken;
        var cliente = elasticsearch.CriarCliente();

        await MapeamentoIndice.CriarIndiceAsync(cliente, "campanhas-teste", comAlias: false, ct);
        await MapeamentoIndice.CriarIndiceAsync(cliente, "campanhas-teste", comAlias: false, ct);

        (await cliente.Indices.ExistsAsync("campanhas-teste", ct)).Exists.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um nome de índice inválido, quando a criação é pedida, então o Elasticsearch recusa e a falha é sinalizada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task CriarIndice_NomeInvalido_LancaExcecao()
    {
        var cliente = elasticsearch.CriarCliente();

        await Should.ThrowAsync<InvalidOperationException>(() => MapeamentoIndice.CriarIndiceAsync(cliente, "Invalido_MAIUSCULO", comAlias: false, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dado um nome vazio ou um cliente nulo, quando a criação é pedida, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task CriarIndice_ArgumentosInvalidos_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => MapeamentoIndice.CriarIndiceAsync(null!, "campanhas-x", comAlias: false, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentException>(() => MapeamentoIndice.CriarIndiceAsync(elasticsearch.Cliente, " ", comAlias: false, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentNullException>(() => MapeamentoIndice.GarantirAliasAsync(null!, TestContext.Current.CancellationToken));
    }
}
