using Bunit;
using ConexaoSolidaria.Web.Components.Pages;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da página inicial: acesso ao painel e à busca, e convite para quem ainda não entrou.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HomeTests : BunitContext
{
    /// <summary>
    /// Dado um visitante, quando a página inicial é exibida, então há a busca (GET /busca), o link do painel e o convite para criar conta.
    /// </summary>
    [Fact]
    public void Inicio_Visitante_MostraBuscaPainelEConvite()
    {
        AddAuthorization();

        var pagina = Render<Home>();

        var formulario = pagina.Find("form[role='search']");
        formulario.GetAttribute("action").ShouldBe("/busca");
        formulario.GetAttribute("method").ShouldBe("get");
        pagina.Find("input[name='q']").GetAttribute("minlength").ShouldBe("2");
        pagina.Find("a[href='/transparencia']").ShouldNotBeNull();
        pagina.Find("a[href='/cadastro']").ShouldNotBeNull();
        pagina.Find("a[href='/entrar']").ShouldNotBeNull();
    }

    /// <summary>
    /// Dado um usuário autenticado, quando a página inicial é exibida, então o convite para criar conta some.
    /// </summary>
    [Fact]
    public void Inicio_UsuarioAutenticado_NaoMostraConvite()
    {
        AddAuthorization().SetAuthorized("Maria");

        var pagina = Render<Home>();

        pagina.FindAll("a[href='/cadastro']").ShouldBeEmpty();
        pagina.Find("a[href='/transparencia']").ShouldNotBeNull();
    }
}
