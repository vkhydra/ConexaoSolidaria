using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Autenticacao;

/// <summary>
/// Testes da validação do JWT RS256 e das policies por papel, com um servidor de teste em memória.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExtensoesAutenticacaoTests : IDisposable
{
    /// <summary>
    /// Chave do emissor legítimo.
    /// </summary>
    private readonly RSA _chave = RSA.Create(2048);

    /// <summary>
    /// Chave de um emissor estranho, para testar assinatura inválida.
    /// </summary>
    private readonly RSA _chaveEstranha = RSA.Create(2048);

    /// <summary>
    /// Dado um token de gestor, quando chama a rota de gestor, então recebe 200.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Gestor_NaRotaDeGestor_Recebe200()
    {
        await using var app = await IniciarAsync();

        var resposta = await ChamarAsync(app, "/gestor", GerarToken("GestorONG"));

        resposta.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dado um token de doador, quando chama a rota de gestor, então recebe 403.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Doador_NaRotaDeGestor_Recebe403()
    {
        await using var app = await IniciarAsync();

        var resposta = await ChamarAsync(app, "/gestor", GerarToken("Doador"));

        resposta.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Dado um token de doador, quando chama a rota de doador, então recebe 200; o de gestor recebe 403.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task RotaDeDoador_ExigePapelDoador()
    {
        await using var app = await IniciarAsync();

        (await ChamarAsync(app, "/doador", GerarToken("Doador"))).ShouldBe(HttpStatusCode.OK);
        (await ChamarAsync(app, "/doador", GerarToken("GestorONG"))).ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Dada uma rota sem policy, quando chamada sem token, então a FallbackPolicy responde 401 com o cabeçalho Bearer.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task RotaSemPolicy_SemToken_Recebe401ComWwwAuthenticate()
    {
        await using var app = await IniciarAsync();

        var resposta = await app.GetTestClient().GetAsync("/qualquer", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        resposta.Headers.WwwAuthenticate.Single().Scheme.ShouldBe("Bearer");
    }

    /// <summary>
    /// Dada uma rota com AllowAnonymous, quando chamada sem token, então recebe 200.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task RotaPublica_SemToken_Recebe200()
    {
        await using var app = await IniciarAsync();

        var resposta = await app.GetTestClient().GetAsync("/publico", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dados tokens inválidos (expirado, assinatura alheia, emissor ou audiência errados), quando usados, então recebem 401.
    /// </summary>
    /// <param name="defeito">Defeito aplicado ao token.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("expirado")]
    [InlineData("assinatura")]
    [InlineData("emissor")]
    [InlineData("audiencia")]
    public async Task TokenInvalido_Recebe401(string defeito)
    {
        await using var app = await IniciarAsync();

        var token = defeito switch
        {
            "expirado" => GerarToken("GestorONG", expiraEm: DateTime.UtcNow.AddMinutes(-5)),
            "assinatura" => GerarToken("GestorONG", chave: _chaveEstranha),
            "emissor" => GerarToken("GestorONG", emissor: "outro-emissor"),
            _ => GerarToken("GestorONG", audiencia: "outra-api"),
        };

        (await ChamarAsync(app, "/gestor", token)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Dado um token que expirou há menos de 30 segundos, quando usado, então ainda é aceito pela tolerância.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task TokenExpiradoDentroDaTolerancia_Recebe200()
    {
        await using var app = await IniciarAsync();

        var token = GerarToken("GestorONG", expiraEm: DateTime.UtcNow.AddSeconds(-10));

        (await ChamarAsync(app, "/gestor", token)).ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dada uma chave pública ausente ou inválida, quando a autenticação é registrada, então a aplicação não sobe.
    /// </summary>
    /// <param name="pem">Conteúdo PEM testado; nulo significa não configurado.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("isto-nao-e-um-pem")]
    public void ChavePublicaAusenteOuInvalida_LancaExcecao(string? pem)
    {
        var builder = AplicacaoDeTeste.CriarConstrutor();
        if (pem is not null)
        {
            builder.Configuration["Jwt:ChavePublicaPem"] = pem;
        }

        Should.Throw<InvalidOperationException>(() => builder.AddAutenticacaoJwt());
    }

    /// <summary>
    /// Dado um construtor nulo, quando a autenticação é registrada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void ConstrutorNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => ExtensoesAutenticacao.AddAutenticacaoJwt(null!));
    }

    /// <summary>
    /// Libera as chaves de teste.
    /// </summary>
    public void Dispose()
    {
        _chave.Dispose();
        _chaveEstranha.Dispose();
    }

    /// <summary>
    /// Sobe uma aplicação com a autenticação JWT e rotas de teste.
    /// </summary>
    /// <returns>Aplicação iniciada no servidor de teste.</returns>
    private async Task<WebApplication> IniciarAsync()
    {
        var builder = AplicacaoDeTeste.CriarConstrutor();
        builder.Configuration["Jwt:ChavePublicaPem"] = _chave.ExportSubjectPublicKeyInfoPem();
        builder.AddAutenticacaoJwt();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/gestor", () => Results.Ok()).RequireAuthorization(ExtensoesAutenticacao.PolicyGestorOng);
        app.MapGet("/doador", () => Results.Ok()).RequireAuthorization(ExtensoesAutenticacao.PolicyDoador);
        app.MapGet("/qualquer", () => Results.Ok());
        app.MapGet("/publico", () => Results.Ok()).AllowAnonymous();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    /// <summary>
    /// Chama uma rota com o token no cabeçalho Authorization.
    /// </summary>
    /// <param name="app">Aplicação de teste.</param>
    /// <param name="rota">Rota chamada.</param>
    /// <param name="token">JWT enviado.</param>
    /// <returns>O status da resposta.</returns>
    private static async Task<HttpStatusCode> ChamarAsync(WebApplication app, string rota, string token)
    {
        var cliente = app.GetTestClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await cliente.GetAsync(rota, TestContext.Current.CancellationToken);
        return resposta.StatusCode;
    }

    /// <summary>
    /// Gera um JWT RS256 com os campos do contrato, permitindo estragar um deles.
    /// </summary>
    /// <param name="role">Papel do usuário.</param>
    /// <param name="expiraEm">Instante de expiração; por padrão, daqui a uma hora.</param>
    /// <param name="chave">Chave de assinatura; por padrão, a do emissor legítimo.</param>
    /// <param name="emissor">Emissor do token.</param>
    /// <param name="audiencia">Audiência do token.</param>
    /// <returns>O JWT assinado.</returns>
    private string GerarToken(string role, DateTime? expiraEm = null, RSA? chave = null, string emissor = "conexao-identity", string audiencia = "conexao-api")
    {
        var expiracao = expiraEm ?? DateTime.UtcNow.AddHours(1);
        var descritor = new SecurityTokenDescriptor
        {
            Issuer = emissor,
            Audience = audiencia,
            NotBefore = expiracao.AddHours(-2),
            Expires = expiracao,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(chave ?? _chave), SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString(), ["role"] = role },
        };

        return new JsonWebTokenHandler().CreateToken(descritor);
    }
}
