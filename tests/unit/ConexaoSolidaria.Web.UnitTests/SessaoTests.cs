using System.Security.Claims;
using System.Text;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da sessão do usuário: leitura do JWT, cookie de autenticação, encerramento com antifalsificação e registro dos serviços.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SessaoTests
{
    /// <summary>
    /// Serviço de autenticação falso, para capturar o login e a saída.
    /// </summary>
    private readonly IAuthenticationService _autenticacao = Substitute.For<IAuthenticationService>();

    /// <summary>
    /// Dado um usuário com a claim do token, quando a sessão é consultada, então devolve o JWT; sem a claim, devolve nulo.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task ObterToken_ComESemClaim_DevolveOuNulo()
    {
        var provedor = Substitute.For<AuthenticationStateProvider>();
        var comToken = new ClaimsPrincipal(new ClaimsIdentity([new Claim(SessaoDoUsuario.ClaimDoToken, "a.b.c")], "teste"));
        provedor.GetAuthenticationStateAsync().Returns(new AuthenticationState(comToken), new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        var sessao = new SessaoDoUsuario(provedor);

        (await sessao.ObterTokenAsync()).ShouldBe("a.b.c");
        (await sessao.ObterTokenAsync()).ShouldBeNull();
    }

    /// <summary>
    /// Dado um login com JWT, quando a sessão é aberta, então o cookie leva nome, papel, sub e o token, e expira com o token.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Entrar_ComLogin_GravaCookieComClaimsEExpiracao()
    {
        var contexto = CriarContexto();
        var jwt = CriarJwt("""{"sub":"0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70","role":"Doador"}""");
        ClaimsPrincipal? principal = null;
        AuthenticationProperties? propriedades = null;
        await _autenticacao.SignInAsync(contexto, "Cookies", Arg.Do<ClaimsPrincipal>(p => principal = p), Arg.Do<AuthenticationProperties>(p => propriedades = p));

        await new ServicoDeSessao().EntrarAsync(contexto, new LoginResposta(jwt, "Bearer", 3600, "Doador"), "maria@exemplo.com");

        principal.ShouldNotBeNull();
        principal.Identity!.Name.ShouldBe("maria@exemplo.com");
        principal.IsInRole("Doador").ShouldBeTrue();
        principal.FindFirst("sub")!.Value.ShouldBe("0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70");
        principal.FindFirst(SessaoDoUsuario.ClaimDoToken)!.Value.ShouldBe(jwt);
        propriedades!.IsPersistent.ShouldBeFalse();
        propriedades.ExpiresUtc!.Value.ShouldBe(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Dado um JWT sem formato válido, quando a sessão é aberta, então o cookie é gravado sem a claim sub.
    /// </summary>
    /// <param name="jwt">Token malformado.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData("nao-e-jwt")]
    [InlineData("a.@@@.c")]
    [InlineData("a.e30.c")]
    public async Task Entrar_JwtMalformado_GravaCookieSemSub(string jwt)
    {
        var contexto = CriarContexto();
        ClaimsPrincipal? principal = null;
        await _autenticacao.SignInAsync(contexto, "Cookies", Arg.Do<ClaimsPrincipal>(p => principal = p), Arg.Any<AuthenticationProperties>());

        await new ServicoDeSessao().EntrarAsync(contexto, new LoginResposta(jwt, "Bearer", 60, "GestorONG"), "Gestora");

        principal!.FindFirst("sub").ShouldBeNull();
        principal.IsInRole("GestorONG").ShouldBeTrue();
    }

    /// <summary>
    /// Dado o encerramento da sessão, quando executado, então o cookie é apagado; argumentos nulos são recusados.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Sair_ApagaOCookie()
    {
        var contexto = CriarContexto();

        await new ServicoDeSessao().SairAsync(contexto);

        await _autenticacao.Received(1).SignOutAsync(contexto, "Cookies", Arg.Any<AuthenticationProperties>());
        await Should.ThrowAsync<ArgumentNullException>(() => new ServicoDeSessao().SairAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => new ServicoDeSessao().EntrarAsync(null!, new LoginResposta("a", "Bearer", 1, "Doador"), "x"));
        await Should.ThrowAsync<ArgumentNullException>(() => new ServicoDeSessao().EntrarAsync(contexto, null!, "x"));
    }

    /// <summary>
    /// Dado um token antifalsificação válido, quando o usuário pede para sair, então a sessão é encerrada e ele volta ao início.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task EndpointSair_TokenValido_EncerraEVoltaAoInicio()
    {
        var contexto = CriarContexto();
        var antiforgery = Substitute.For<IAntiforgery>();
        var sessao = Substitute.For<IServicoDeSessao>();

        var resultado = await EndpointsDeSessao.SairAsync(contexto, antiforgery, sessao);

        resultado.ShouldBeOfType<RedirectHttpResult>().Url.ShouldBe("/");
        await sessao.Received(1).SairAsync(contexto);
    }

    /// <summary>
    /// Dado um token antifalsificação inválido, quando o usuário pede para sair, então recebe 400 e a sessão continua.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task EndpointSair_TokenInvalido_Recebe400ESessaoContinua()
    {
        var contexto = CriarContexto();
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.ValidateRequestAsync(contexto).Returns(_ => throw new AntiforgeryValidationException("token inválido"));
        var sessao = Substitute.For<IServicoDeSessao>();

        var resultado = await EndpointsDeSessao.SairAsync(contexto, antiforgery, sessao);

        resultado.ShouldBeOfType<BadRequest>();
        await sessao.DidNotReceiveWithAnyArgs().SairAsync(default!);
    }

    /// <summary>
    /// Dada a configuração das duas APIs, quando os serviços são registrados, então clientes e sessão são resolvidos.
    /// </summary>
    [Fact]
    public void AddServicosDaApi_ComConfiguracao_RegistraClientesESessao()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:Identity"] = "http://identity/", ["Api:Campanhas"] = "http://campanhas/" }).Build();
        var servicos = new ServiceCollection().AddLogging().AddSingleton(Substitute.For<AuthenticationStateProvider>());

        servicos.AddServicosDaApi(configuracao);

        using var provedor = servicos.BuildServiceProvider();
        using var escopo = provedor.CreateScope();
        escopo.ServiceProvider.GetRequiredService<IClienteIdentity>().ShouldBeOfType<ClienteIdentity>();
        escopo.ServiceProvider.GetRequiredService<IClienteCampanhas>().ShouldBeOfType<ClienteCampanhas>();
        provedor.GetRequiredService<IServicoDeSessao>().ShouldBeOfType<ServicoDeSessao>();
    }

    /// <summary>
    /// Dada a configuração incompleta ou argumentos nulos, quando os serviços são registrados, então a aplicação não sobe.
    /// </summary>
    [Fact]
    public void AddServicosDaApi_ConfiguracaoInvalida_LancaExcecao()
    {
        var soIdentity = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:Identity"] = "http://identity/" }).Build();
        var vazia = new ConfigurationBuilder().Build();

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddServicosDaApi(vazia));
        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddServicosDaApi(soIdentity));
        Should.Throw<ArgumentNullException>(() => ExtensoesWeb.AddServicosDaApi(null!, vazia));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddServicosDaApi(null!));
    }

    /// <summary>
    /// Cria um contexto HTTP com o serviço de autenticação falso.
    /// </summary>
    /// <returns>O contexto.</returns>
    private DefaultHttpContext CriarContexto()
    {
        return new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(_autenticacao).BuildServiceProvider() };
    }

    /// <summary>
    /// Monta um JWT sem assinatura válida com o payload informado.
    /// </summary>
    /// <param name="payload">Payload JSON.</param>
    /// <returns>O token.</returns>
    private static string CriarJwt(string payload)
    {
        static string Base64Url(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Base64Url("""{"alg":"none"}""")}.{Base64Url(payload)}.assinatura";
    }
}
