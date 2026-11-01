using Bunit;
using ConexaoSolidaria.Web.Components.Layout;
using ConexaoSolidaria.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes do layout, do menu por perfil e da página de erro.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LayoutTests : BunitContext
{
    /// <summary>
    /// Dado um visitante, quando o menu é exibido, então vê Entrar e Criar conta, e nada de gestão, dados ou saída.
    /// </summary>
    [Fact]
    public void Menu_Visitante_VeEntrarECriarConta()
    {
        AddAuthorization();

        var menu = Render<NavMenu>();

        menu.Find("a[href='/entrar']").TextContent.ShouldBe("Entrar");
        menu.Find("a[href='/cadastro']").TextContent.ShouldBe("Criar conta");
        menu.Find("a[href='/transparencia']").ShouldNotBeNull();
        menu.Find("a[href='/busca']").ShouldNotBeNull();
        menu.FindAll("a[href='/gestao/campanhas']").ShouldBeEmpty();
        menu.FindAll("a[href='/meus-dados']").ShouldBeEmpty();
        menu.FindAll("form[action='/sair']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um doador autenticado, quando o menu é exibido, então vê Meus dados e Sair, e não vê Gestão nem Entrar.
    /// </summary>
    [Fact]
    public void Menu_Doador_VeMeusDadosESair()
    {
        var autorizacao = AddAuthorization();
        autorizacao.SetAuthorized("Maria");
        autorizacao.SetRoles("Doador");

        var menu = Render<NavMenu>();

        menu.Find("a[href='/meus-dados']").TextContent.ShouldBe("Meus dados");
        menu.Find("form[action='/sair'] button").TextContent.ShouldContain("Maria");
        menu.FindAll("a[href='/gestao/campanhas']").ShouldBeEmpty();
        menu.FindAll("a[href='/entrar']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um gestor autenticado, quando o menu é exibido, então vê Gestão e não vê Meus dados.
    /// </summary>
    [Fact]
    public void Menu_Gestor_VeGestao()
    {
        var autorizacao = AddAuthorization();
        autorizacao.SetAuthorized("Gestora");
        autorizacao.SetRoles("GestorONG");

        var menu = Render<NavMenu>();

        menu.Find("a[href='/gestao/campanhas']").TextContent.ShouldBe("Gestão");
        menu.FindAll("a[href='/meus-dados']").ShouldBeEmpty();
        menu.Find("form[action='/sair']").ShouldNotBeNull();
    }

    /// <summary>
    /// Dado um conteúdo, quando o layout é exibido, então mostra o menu, o conteúdo e o link da política de privacidade.
    /// </summary>
    [Fact]
    public void Layout_ComConteudo_MostraMenuCorpoERodape()
    {
        AddAuthorization();

        var layout = Render<MainLayout>(parametros => parametros.Add(p => p.Body, (RenderFragment)(construtor => construtor.AddMarkupContent(0, "<p id=\"corpo\">Olá</p>"))));

        layout.Find("#corpo").TextContent.ShouldBe("Olá");
        layout.Find("header.topo").ShouldNotBeNull();
        layout.Find("footer a[href='/politica-de-privacidade']").ShouldNotBeNull();
    }

    /// <summary>
    /// Dada a página de erro sem código, quando exibida, então não mostra o código de rastreio.
    /// </summary>
    [Fact]
    public void Erro_SemCodigo_NaoMostraRastreio()
    {
        var pagina = Render<Erro>();

        pagina.Find("h1").TextContent.ShouldBe("Algo deu errado");
        pagina.FindAll("[data-teste='rastreio']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada a página de erro com código de rastreio, quando exibida, então mostra o código para o usuário informar à equipe.
    /// </summary>
    [Fact]
    public void Erro_ComCodigo_MostraRastreio()
    {
        var navegacao = Services.GetRequiredService<NavigationManager>();
        navegacao.NavigateTo(navegacao.GetUriWithQueryParameter("rastreio", "4bf92f3577b34da6a3ce929d0e0e4736"));

        var pagina = Render<Erro>();

        pagina.Find("[data-teste='rastreio']").TextContent.ShouldBe("4bf92f3577b34da6a3ce929d0e0e4736");
    }
}
