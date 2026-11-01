using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da página de busca e do tratamento seguro do trecho destacado.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BuscaTests : BunitContext
{
    /// <summary>
    /// Cliente de Campanhas falso.
    /// </summary>
    private readonly IClienteCampanhas _campanhas = Substitute.For<IClienteCampanhas>();

    /// <summary>
    /// Registra o falso no contêiner do bUnit.
    /// </summary>
    public BuscaTests()
    {
        Services.AddSingleton(_campanhas);
    }

    /// <summary>
    /// Dada a página sem consulta, quando exibida, então mostra só o formulário e não chama a API.
    /// </summary>
    [Fact]
    public void Pagina_SemConsulta_MostraSoOFormulario()
    {
        var pagina = Render<Busca>();

        pagina.Find("form[role='search']").GetAttribute("method").ShouldBe("get");
        pagina.FindAll("[data-teste='resultado'], [data-teste='mensagem'], [data-teste='vazio']").ShouldBeEmpty();
        _campanhas.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um texto, quando a página é aberta, então mostra os resultados com o termo destacado e o link para doar.
    /// </summary>
    [Fact]
    public void Pagina_ComResultados_MostraTituloTrechoEDoar()
    {
        var id = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");
        _campanhas.BuscarAsync("natl", 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Resposta(1, 1, new BuscaItem(id, "Natal Solidário", "Presentes e <em>ceia</em> para 120 crianças", 3.4, new DateOnly(2026, 12, 24)))));
        Navegar("/busca?q=natl");

        var pagina = Render<Busca>();

        var cartao = pagina.Find("[data-teste='resultado']");
        cartao.QuerySelector("h2")!.TextContent.ShouldBe("Natal Solidário");
        cartao.QuerySelector("mark")!.TextContent.ShouldBe("ceia");
        cartao.QuerySelector("[data-teste='trecho']")!.TextContent.Trim().ShouldBe("Presentes e ceia para 120 crianças");
        cartao.QuerySelector("a")!.GetAttribute("href").ShouldBe($"/campanhas/{id}/doar");
        pagina.Find("[data-teste='total']").TextContent.ShouldBe("10 resultado(s)");
        pagina.FindAll("[data-teste='anterior'], [data-teste='proxima']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um trecho com HTML malicioso, quando exibido, então nenhuma tag além do destaque chega ao navegador.
    /// </summary>
    [Fact]
    public void Pagina_TrechoComHtml_NaoGeraTagsPerigosas()
    {
        var trecho = "Veja <em>ceia</em> &lt;script&gt;alert(1)&lt;/script&gt; &lt;img src=x onerror=alert(1)&gt;";
        _campanhas.BuscarAsync("ceia", 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Resposta(1, 1, new BuscaItem(Guid.NewGuid(), "Ceia", trecho, 1, new DateOnly(2026, 12, 24)))));
        Navegar("/busca?q=ceia");

        var pagina = Render<Busca>();

        pagina.FindAll("script, img").ShouldBeEmpty();
        pagina.Find("[data-teste='trecho']").TextContent.ShouldContain("<script>alert(1)</script>");
        pagina.Find("[data-teste='trecho']").InnerHtml.ShouldContain("&lt;script&gt;");
        pagina.FindAll("[data-teste='trecho'] mark").Count.ShouldBe(1);
    }

    /// <summary>
    /// Dado um resultado com várias páginas, quando exibido, então há links para a anterior e a próxima, com o texto codificado.
    /// </summary>
    [Fact]
    public void Pagina_ComVariasPaginas_MostraNavegacao()
    {
        _campanhas.BuscarAsync("natal solidário", 2, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Resposta(2, 3, new BuscaItem(Guid.NewGuid(), "Natal", null, 1, new DateOnly(2026, 12, 24)))));
        Navegar("/busca?q=natal%20solid%C3%A1rio&page=2");

        var pagina = Render<Busca>();

        pagina.Find("[data-teste='anterior']").GetAttribute("href").ShouldBe("/busca?q=natal%20solid%C3%A1rio&page=1");
        pagina.Find("[data-teste='proxima']").GetAttribute("href").ShouldBe("/busca?q=natal%20solid%C3%A1rio&page=3");
        pagina.FindAll("[data-teste='trecho']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um resultado vazio, quando exibido, então mostra que nada foi encontrado.
    /// </summary>
    [Fact]
    public void Pagina_SemResultados_MostraMensagemDeVazio()
    {
        _campanhas.BuscarAsync("zzz", 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new BuscaResposta([], 1, 10, 0, 0)));
        Navegar("/busca?q=zzz");

        var pagina = Render<Busca>();

        pagina.Find("[data-teste='vazio']").TextContent.ShouldBe("Nenhuma campanha encontrada para esta busca.");
    }

    /// <summary>
    /// Dado um texto curto demais, quando a página é aberta, então avisa sem chamar a API.
    /// </summary>
    [Fact]
    public void Pagina_TextoCurto_AvisaSemChamarAApi()
    {
        Navegar("/busca?q=a");

        var pagina = Render<Busca>();

        pagina.Find("[data-teste='mensagem']").TextContent.ShouldBe("Digite ao menos 2 letras para buscar.");
        _campanhas.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>
    /// Dadas falhas da API, quando a página é aberta, então a mensagem é a do manual: indisponível, validação ou a da API.
    /// </summary>
    /// <param name="status">Status da API.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    [Theory]
    [InlineData(503, "Busca temporariamente indisponível. Use o painel de transparência ou tente mais tarde.")]
    [InlineData(400, "Digite entre 2 e 100 caracteres para buscar.")]
    [InlineData(500, "Falha qualquer.")]
    public void Pagina_FalhaDaApi_MostraMensagemAdequada(int status, string mensagem)
    {
        _campanhas.BuscarAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<BuscaResposta>(new ErroApi(status, null, "Falha qualquer.", new Dictionary<string, string[]>(), null)));
        Navegar("/busca?q=natal");

        var pagina = Render<Busca>();

        pagina.Find("[data-teste='mensagem']").TextContent.ShouldBe(mensagem);
    }

    /// <summary>
    /// Dados trechos variados, quando segmentados, então só a tag em vira destaque e entidades viram texto.
    /// </summary>
    /// <param name="trecho">Trecho de entrada.</param>
    /// <param name="esperado">Segmentos esperados, no formato <c>texto|D</c> para destaque e <c>texto|N</c> para texto comum.</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("sem marcação", "sem marcação|N")]
    [InlineData("<em>só termo</em>", "só termo|D")]
    [InlineData("a <em>b</em> c <em>d</em>", "a |N;b|D; c |N;d|D")]
    [InlineData("a &amp; b", "a & b|N")]
    [InlineData("abre <em>sem fechar", "abre |N;sem fechar|N")]
    [InlineData("<em></em>fim", "fim|N")]
    public void Segmentar_Trechos_SeparaDestaqueDeTexto(string? trecho, string esperado)
    {
        var segmentos = TrechoDestacado.Segmentar(trecho).Select(s => $"{s.Texto}|{(s.Destaque ? 'D' : 'N')}");

        string.Join(';', segmentos).ShouldBe(esperado);
    }

    /// <summary>
    /// Cria uma resposta de busca com um item.
    /// </summary>
    /// <param name="pagina">Página atual.</param>
    /// <param name="totalDePaginas">Total de páginas.</param>
    /// <param name="item">Item da página.</param>
    /// <returns>A resposta.</returns>
    private static BuscaResposta Resposta(int pagina, int totalDePaginas, BuscaItem item)
    {
        return new BuscaResposta([item], pagina, 10, totalDePaginas * 10, totalDePaginas);
    }

    /// <summary>
    /// Navega para um endereço, para que os parâmetros de consulta cheguem à página.
    /// </summary>
    /// <param name="caminho">Caminho com a consulta.</param>
    private void Navegar(string caminho)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(caminho);
    }
}
