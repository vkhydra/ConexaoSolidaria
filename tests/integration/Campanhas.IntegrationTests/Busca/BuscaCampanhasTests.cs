using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Elastic.Clients.Elasticsearch;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes da busca de campanhas num Elasticsearch real: erro de digitação, pesos, filtro de status, acentos, destaque,
/// caracteres especiais, paginação e falhas.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class BuscaCampanhasTests(ElasticsearchFixture elasticsearch)
{
    /// <summary>
    /// Dado "Natal Solidário" indexado, quando se busca "natl solidaro", então a campanha é achada apesar dos erros de digitação.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_ErroDeDigitacao_AchaACampanha()
    {
        var busca = await PrepararAsync(Documento("Natal Solidário", "Presentes para crianças"));

        var pagina = await busca.BuscarAsync("natl solidaro", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.Select(i => i.Titulo).ShouldBe(["Natal Solidário"]);
        pagina.Total.ShouldBe(1);
    }

    /// <summary>
    /// Dado "pascoa" sem acento, quando a campanha se chama "Páscoa Feliz", então ela é achada (acento e caixa não importam).
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_SemAcento_AchaTituloComAcento()
    {
        var busca = await PrepararAsync(Documento("Páscoa Feliz", "Chocolates"));

        var pagina = await busca.BuscarAsync("PASCOA", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.Select(i => i.Titulo).ShouldBe(["Páscoa Feliz"]);
    }

    /// <summary>
    /// Dadas duas campanhas, quando o termo aparece no título de uma e só na descrição da outra, então o título vem primeiro.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_TituloPesaMaisQueDescricao_TituloVemPrimeiro()
    {
        var busca = await PrepararAsync(
            Documento("Agasalho", "Roupas de inverno"),
            Documento("Inverno sem fome", "Cestas básicas"));

        var pagina = await busca.BuscarAsync("inverno", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.Select(i => i.Titulo).ShouldBe(["Inverno sem fome", "Agasalho"]);
        pagina.Itens[0].Relevancia.ShouldBeGreaterThan(pagina.Itens[1].Relevancia);
    }

    /// <summary>
    /// Dadas campanhas cancelada e concluída, quando se busca o termo delas, então só as ativas aparecem.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_CampanhaNaoAtiva_NaoAparece()
    {
        var busca = await PrepararAsync(
            Documento("Inverno 2026", "Cancelada", status: "Cancelada"),
            Documento("Inverno 2025", "Concluída", status: "Concluida"));

        var pagina = await busca.BuscarAsync("inverno", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.ShouldBeEmpty();
        pagina.Total.ShouldBe(0);
    }

    /// <summary>
    /// Dado um texto só de palavras vazias, quando se busca, então o resultado é vazio e não há erro.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_SoPalavrasVazias_DevolveVazio()
    {
        var busca = await PrepararAsync(Documento("Natal Solidário", "Presentes para as crianças"));

        var pagina = await busca.BuscarAsync("de a", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dados textos com sintaxe de consulta e aspas, quando se busca, então tudo é tratado como texto e não há erro.
    /// </summary>
    /// <param name="texto">Texto digitado.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData("natal AND (")]
    [InlineData("*:* OR titulo:natal")]
    [InlineData("\"natal\" -solidario ~2 /regex/")]
    [InlineData("natal\\")]
    public async Task Buscar_CaracteresEspeciais_SaoTextoESemErro(string texto)
    {
        var busca = await PrepararAsync(Documento("Natal Solidário", "Presentes"));

        var pagina = await busca.BuscarAsync(texto, 1, 10, TestContext.Current.CancellationToken);

        pagina.Total.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Dada uma descrição com termo e HTML, quando se busca o termo, então o trecho destaca com &lt;em&gt; e escapa o resto do HTML.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_Trecho_DestacaComEmEEscapaHtml()
    {
        var busca = await PrepararAsync(Documento("Ceia solidária", "Presentes e ceia para <script>alert(1)</script> 120 crianças"));

        var pagina = await busca.BuscarAsync("ceia", 1, 10, TestContext.Current.CancellationToken);

        var trecho = pagina.Itens.ShouldHaveSingleItem().Trecho;
        trecho.ShouldNotBeNull();
        trecho.ShouldContain("<em>ceia</em>");
        trecho.ShouldNotContain("<script>");
        trecho.ShouldContain("&lt;script&gt;");
    }

    /// <summary>
    /// Dado um termo que só casa com o título, quando se busca, então o trecho é nulo.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_CasaSoNoTitulo_TrechoNulo()
    {
        var busca = await PrepararAsync(Documento("Horta comunitária", "Verduras para a vizinhança"));

        var pagina = await busca.BuscarAsync("horta", 1, 10, TestContext.Current.CancellationToken);

        pagina.Itens.ShouldHaveSingleItem().Trecho.ShouldBeNull();
    }

    /// <summary>
    /// Dadas 12 campanhas iguais, quando se pede a segunda página de 5, então vêm 5 itens e o total é 12.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_Paginacao_RespeitaPaginaETotal()
    {
        var busca = await PrepararAsync([.. Enumerable.Range(1, 12).Select(n => Documento($"Mutirão {n}", "Limpeza da praça", dataFim: new DateOnly(2026, 12, n)))]);

        var segunda = await busca.BuscarAsync("mutirão", 2, 5, TestContext.Current.CancellationToken);
        var terceira = await busca.BuscarAsync("mutirão", 3, 5, TestContext.Current.CancellationToken);

        segunda.Itens.Count.ShouldBe(5);
        segunda.Total.ShouldBe(12);
        terceira.Itens.Count.ShouldBe(2);
    }

    /// <summary>
    /// Dado um Elasticsearch sem nenhum índice ainda, quando se busca, então o resultado é vazio e não há erro.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_SemIndiceAinda_DevolveVazio()
    {
        var ct = TestContext.Current.CancellationToken;
        await elasticsearch.Cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);

        var pagina = await new BuscaCampanhasElasticsearch(elasticsearch.Cliente).BuscarAsync("natal", 1, 10, ct);

        pagina.Itens.ShouldBeEmpty();
        pagina.Total.ShouldBe(0);
    }

    /// <summary>
    /// Dado um Elasticsearch inalcançável, quando se busca, então a falha vira <see cref="BuscaIndisponivelException"/>.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_ElasticsearchInalcancavel_LancaIndisponivel()
    {
        var busca = new BuscaCampanhasElasticsearch(new ElasticsearchClient(new Uri("http://127.0.0.1:1")));

        await Should.ThrowAsync<BuscaIndisponivelException>(() => busca.BuscarAsync("natal", 1, 10, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dado um texto vazio, quando se busca, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_TextoVazio_LancaExcecao()
    {
        var busca = new BuscaCampanhasElasticsearch(elasticsearch.Cliente);

        await Should.ThrowAsync<ArgumentException>(() => busca.BuscarAsync(" ", 1, 10, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Recria o índice, grava os documentos e espera o Elasticsearch torná-los pesquisáveis.
    /// </summary>
    /// <param name="documentos">Documentos a indexar.</param>
    /// <returns>A busca sobre o índice recém-preenchido.</returns>
    private async Task<BuscaCampanhasElasticsearch> PrepararAsync(params CampanhaIndexada[] documentos)
    {
        var ct = TestContext.Current.CancellationToken;
        await elasticsearch.Cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);
        foreach (var documento in documentos)
        {
            await indice.GravarAsync(documento, ct);
        }

        await elasticsearch.Cliente.Indices.RefreshAsync(MapeamentoIndice.NomeAlias, ct);
        return new BuscaCampanhasElasticsearch(elasticsearch.Cliente);
    }

    /// <summary>
    /// Cria um documento de campanha.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="descricao">Descrição.</param>
    /// <param name="status">Situação; <c>Ativa</c> por padrão.</param>
    /// <param name="dataFim">Último dia; 24/12/2026 por padrão.</param>
    /// <returns>O documento.</returns>
    private static CampanhaIndexada Documento(string titulo, string descricao, string status = "Ativa", DateOnly? dataFim = null)
    {
        return new CampanhaIndexada(Guid.NewGuid(), titulo, descricao, status, 1000.00m, dataFim ?? new DateOnly(2026, 12, 24), 1);
    }
}
