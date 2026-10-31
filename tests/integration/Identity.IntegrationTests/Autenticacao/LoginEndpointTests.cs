using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.IntegrationTests.Suporte;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Identity.IntegrationTests.Autenticacao;

/// <summary>
/// Testes de ponta a ponta do <c>POST /api/v1/auth/login</c>: API real, PostgreSQL em container e JWT verdadeiro.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class LoginEndpointTests(IdentityApiFixture api) : IClassFixture<IdentityApiFixture>
{
    /// <summary>
    /// _rota do login.
    /// </summary>
    private const string _rota = "/api/v1/auth/login";

    /// <summary>
    /// _senha dos usuários de teste.
    /// </summary>
    private const string _senha = "Senha@123";

    /// <summary>
    /// Dado um gestor cadastrado, quando faz login com e-mail em outra caixa, então recebe 200 e um token sem dados pessoais.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Login_CredenciaisValidas_Devolve200ComToken()
    {
        var email = await api.CriarGestorAsync(_senha);
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(_rota, new { email = email.ToUpperInvariant(), senha = _senha }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("tokenType").GetString().ShouldBe("Bearer");
        corpo.GetProperty("expiresIn").GetInt32().ShouldBe(3600);
        corpo.GetProperty("role").GetString().ShouldBe("GestorONG");

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(corpo.GetProperty("accessToken").GetString());
        jwt.GetClaim("role").Value.ShouldBe("GestorONG");
        jwt.EncodedPayload.ShouldNotContain("exemplo");
    }

    /// <summary>
    /// Dada uma senha errada, quando faz login, então recebe 401 com a mensagem genérica e o código estável.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Login_SenhaIncorreta_Devolve401()
    {
        var email = await api.CriarGestorAsync(_senha);
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(_rota, new { email, senha = "errada" }, TestContext.Current.CancellationToken);

        await ConferirProblemaAsync(resposta, HttpStatusCode.Unauthorized, "auth.credenciais_invalidas", "E-mail ou senha inválidos");
    }

    /// <summary>
    /// Dado um e-mail inexistente, quando faz login, então recebe a mesma resposta 401 da senha errada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Login_EmailInexistente_Devolve401IgualASenhaErrada()
    {
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(_rota, new { email = "ninguem@exemplo.com", senha = _senha }, TestContext.Current.CancellationToken);

        await ConferirProblemaAsync(resposta, HttpStatusCode.Unauthorized, "auth.credenciais_invalidas", "E-mail ou senha inválidos");
    }

    /// <summary>
    /// Dadas cinco falhas seguidas, quando tenta entrar com a senha correta, então recebe 429.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Login_AposCincoFalhas_Devolve429MesmoComSenhaCorreta()
    {
        var email = await api.CriarGestorAsync(_senha);
        using var cliente = api.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var falha = await cliente.PostAsJsonAsync(_rota, new { email, senha = "errada" }, TestContext.Current.CancellationToken);
            falha.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var resposta = await cliente.PostAsJsonAsync(_rota, new { email, senha = _senha }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("code").GetString().ShouldBe("auth.bloqueado");
    }

    /// <summary>
    /// Dado um corpo sem e-mail nem senha, quando faz login, então recebe 400 com os erros por campo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Login_CorpoVazio_Devolve400ComErrosPorCampo()
    {
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(_rota, new { }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("code").GetString().ShouldBe("validacao");
        corpo.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["email", "senha"], ignoreOrder: true);
    }

    /// <summary>
    /// Dada uma rota inexistente que exige autenticação, quando chamada sem token, então a FallbackPolicy responde 401.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task RotaProtegida_SemToken_Devolve401()
    {
        using var cliente = api.CreateClient();

        var resposta = await cliente.GetAsync("/api/v1/qualquer", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Confere o status, o código estável e o título de uma resposta de erro.
    /// </summary>
    /// <param name="resposta">Resposta HTTP.</param>
    /// <param name="status">Status esperado.</param>
    /// <param name="codigo">Código estável esperado.</param>
    /// <param name="titulo">Título esperado.</param>
    /// <returns>Tarefa da conferência.</returns>
    private static async Task ConferirProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo, string titulo)
    {
        resposta.StatusCode.ShouldBe(status);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("code").GetString().ShouldBe(codigo);
        corpo.GetProperty("title").GetString().ShouldBe(titulo);
    }
}
