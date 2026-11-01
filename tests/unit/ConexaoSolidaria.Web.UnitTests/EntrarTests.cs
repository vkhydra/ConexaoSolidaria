using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da página de login: formulário, mensagens de erro sem revelar qual dado errou, sessão aberta e destino seguro.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EntrarTests : BunitContext
{
    /// <summary>
    /// Cliente da Identity falso.
    /// </summary>
    private readonly IClienteIdentity _identity = Substitute.For<IClienteIdentity>();

    /// <summary>
    /// Serviço de sessão falso.
    /// </summary>
    private readonly IServicoDeSessao _sessao = Substitute.For<IServicoDeSessao>();

    /// <summary>
    /// Contexto HTTP da requisição simulada.
    /// </summary>
    private readonly DefaultHttpContext _contexto = new();

    /// <summary>
    /// Registra os falsos no contêiner do bUnit.
    /// </summary>
    public EntrarTests()
    {
        Services.AddSingleton(_identity);
        Services.AddSingleton(_sessao);
        AddAuthorization();
    }

    /// <summary>
    /// Dada a página de login, quando exibida, então tem e-mail, senha, botão e o link para criar conta.
    /// </summary>
    [Fact]
    public void Pagina_Exibida_TemFormularioELinkDeCadastro()
    {
        var pagina = Render<Entrar>();

        pagina.Find("h1").TextContent.ShouldBe("Entrar");
        pagina.Find("input[type='email']").ShouldNotBeNull();
        pagina.Find("input[type='password']").ShouldNotBeNull();
        pagina.Find("button[type='submit']").TextContent.ShouldBe("Entrar");
        pagina.Find("a[href='/cadastro']").ShouldNotBeNull();
        pagina.FindAll("[data-teste='mensagem']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um login válido, quando enviado, então a sessão é aberta com o JWT e o usuário vai ao início.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Enviar_LoginValido_AbreASessaoEVaiAoInicio()
    {
        var login = new LoginResposta("a.b.c", "Bearer", 3600, "Doador");
        _identity.LoginAsync("maria@exemplo.com", "Senha@123", Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(login));
        var pagina = RenderizarPagina();
        pagina.Instance.Modelo!.Email = "maria@exemplo.com";
        pagina.Instance.Modelo.Senha = "Senha@123";

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);

        await _sessao.Received(1).EntrarAsync(_contexto, login, "maria@exemplo.com");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/");
        pagina.Instance.Mensagem.ShouldBeNull();
    }

    /// <summary>
    /// Dado um destino local de retorno, quando o login dá certo, então o usuário volta para ele; destino externo é ignorado.
    /// </summary>
    /// <param name="retorno">Valor do parâmetro de retorno.</param>
    /// <param name="esperado">Destino esperado.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData("/gestao/campanhas", "/gestao/campanhas")]
    [InlineData("https://mal.example/", "/")]
    [InlineData("//mal.example", "/")]
    [InlineData("/\\mal.example", "/")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void DestinoSeguro_AceitaSoEnderecoLocal(string? retorno, string esperado)
    {
        Entrar.DestinoSeguro(retorno).ShouldBe(esperado);
    }

    /// <summary>
    /// Dado um login recusado, quando enviado, então a mensagem é a mesma para qualquer credencial errada e a sessão não abre.
    /// </summary>
    /// <param name="status">Status devolvido pela API.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData(401, "E-mail ou senha inválidos.")]
    [InlineData(429, "Muitas tentativas. Tente novamente em alguns minutos.")]
    [InlineData(400, "Informe um e-mail e uma senha válidos.")]
    [InlineData(503, "O serviço está indisponível. Código de rastreio: t1.")]
    public async Task Enviar_LoginRecusado_MostraMensagemSemAbrirSessao(int status, string mensagem)
    {
        _identity.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<LoginResposta>(new ErroApi(status, null, "O serviço está indisponível.", new Dictionary<string, string[]>(), "t1")));
        var pagina = RenderizarPagina();
        pagina.Instance.Modelo!.Email = "x@x.com";
        pagina.Instance.Modelo.Senha = "errada";

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);
        pagina.Render();

        pagina.Find("[data-teste='mensagem']").TextContent.ShouldBe(mensagem);
        await _sessao.DidNotReceiveWithAnyArgs().EntrarAsync(default!, default!, default!);
    }

    /// <summary>
    /// Dada uma falha sem código de rastreio, quando o login é recusado, então a mensagem da API é mostrada sozinha.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Enviar_FalhaSemRastreio_MostraSoAMensagem()
    {
        _identity.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<LoginResposta>(new ErroApi(500, null, "Erro interno.", new Dictionary<string, string[]>(), null)));
        var pagina = RenderizarPagina();

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);

        pagina.Instance.Mensagem.ShouldBe("Erro interno.");
    }

    /// <summary>
    /// Renderiza a página com o contexto HTTP cascateado, como no SSR.
    /// </summary>
    /// <returns>O componente renderizado.</returns>
    private IRenderedComponent<Entrar> RenderizarPagina()
    {
        return Render<Entrar>(parametros => parametros.AddCascadingValue<HttpContext>(_contexto));
    }
}
