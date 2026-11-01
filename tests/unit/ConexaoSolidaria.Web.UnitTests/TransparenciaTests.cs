using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes do painel de transparência e da formatação em pt-BR.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TransparenciaTests : BunitContext
{
    /// <summary>
    /// Cliente de Campanhas falso.
    /// </summary>
    private readonly IClienteCampanhas _campanhas = Substitute.For<IClienteCampanhas>();

    /// <summary>
    /// Registra o falso no contêiner do bUnit.
    /// </summary>
    public TransparenciaTests()
    {
        Services.AddSingleton(_campanhas);
    }

    /// <summary>
    /// Dadas campanhas ativas, quando o painel é exibido, então cada cartão mostra meta, total, barra de progresso e prazo.
    /// </summary>
    [Fact]
    public void Painel_ComCampanhas_MostraCartoesComProgresso()
    {
        var id = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");
        IReadOnlyList<PainelItem> itens = [new PainelItem(id, "Natal Solidário", 10000m, 150m, 1.5m, new DateOnly(2026, 12, 24)), new PainelItem(Guid.NewGuid(), "Meta batida", 100m, 130m, 130m, new DateOnly(2027, 1, 5))];
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(itens));

        var pagina = Render<Transparencia>();

        var cartoes = pagina.FindAll("[data-teste='campanha']");
        cartoes.Count.ShouldBe(2);
        cartoes[0].QuerySelector("h2")!.TextContent.ShouldBe("Natal Solidário");
        cartoes[0].TextContent.ShouldContain("R$ 10.000,00");
        cartoes[0].TextContent.ShouldContain("R$ 150,00");
        cartoes[0].TextContent.ShouldContain("1,5% da meta");
        cartoes[0].TextContent.ShouldContain("24/12/2026");
        cartoes[0].QuerySelector("progress")!.GetAttribute("value").ShouldBe("1.5");
        cartoes[0].QuerySelector("a")!.GetAttribute("href").ShouldBe($"/campanhas/{id}/doar");
        cartoes[1].QuerySelector("progress")!.GetAttribute("value").ShouldBe("100");
        pagina.Find("[data-teste='aviso']").TextContent.ShouldContain("em até alguns segundos");
    }

    /// <summary>
    /// Dado um painel sem campanhas, quando exibido, então mostra a mensagem de vazio.
    /// </summary>
    [Fact]
    public void Painel_SemCampanhas_MostraMensagemDeVazio()
    {
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok<IReadOnlyList<PainelItem>>([]));

        var pagina = Render<Transparencia>();

        pagina.Find("[data-teste='vazio']").TextContent.ShouldBe("Não há campanhas ativas no momento.");
        pagina.FindAll("[data-teste='campanha']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma falha da API, quando o painel é exibido, então mostra o aviso de indisponibilidade e nenhuma lista.
    /// </summary>
    [Fact]
    public void Painel_ApiIndisponivel_MostraAvisoDeIndisponibilidade()
    {
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha<IReadOnlyList<PainelItem>>(new ErroApi(503, "servico.indisponivel", "fora", new Dictionary<string, string[]>(), null)));

        var pagina = Render<Transparencia>();

        pagina.Find("[data-teste='mensagem']").TextContent.ShouldContain("temporariamente indisponível");
        pagina.FindAll("[data-teste='campanha']").ShouldBeEmpty();
        pagina.FindAll("[data-teste='vazio']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dados valores, datas e percentuais, quando formatados, então seguem o padrão do Brasil.
    /// </summary>
    [Fact]
    public void Formatacao_ValoresDatasEPercentuais_SeguemOPadraoBrasileiro()
    {
        Formatacao.Moeda(1234.5m).ShouldBe("R$ 1.234,50");
        Formatacao.Moeda(0m).ShouldBe("R$ 0,00");
        Formatacao.Data(new DateOnly(2026, 11, 3)).ShouldBe("03/11/2026");
        Formatacao.Percentual(1.5m).ShouldBe("1,5%");
        Formatacao.Percentual(100m).ShouldBe("100%");
    }
}
