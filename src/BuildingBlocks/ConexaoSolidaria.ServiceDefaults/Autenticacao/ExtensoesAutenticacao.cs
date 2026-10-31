using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ConexaoSolidaria.ServiceDefaults.Autenticacao;

/// <summary>
/// Validação do JWT RS256 emitido pela Identity.Api e as policies de autorização por papel, iguais em todos os serviços.
/// </summary>
public static class ExtensoesAutenticacao
{
    /// <summary>
    /// Nome da policy que exige o papel <c>GestorONG</c>.
    /// </summary>
    public const string PolicyGestorOng = "GestorONG";

    /// <summary>
    /// Nome da policy que exige o papel <c>Doador</c>.
    /// </summary>
    public const string PolicyDoador = "Doador";

    /// <summary>
    /// Nome da claim que carrega o papel do usuário.
    /// </summary>
    public const string ClaimRole = "role";

    /// <summary>
    /// Registra a autenticação JWT Bearer, as policies <c>GestorONG</c> e <c>Doador</c> e a <c>FallbackPolicy</c>
    /// que exige usuário autenticado em todo endpoint sem política própria. Endpoint público precisa de <c>AllowAnonymous()</c>.
    /// A chave pública vem de <c>Jwt:ChavePublicaPem</c> (<c>Jwt__ChavePublicaPem</c>); emissor e audiência, de <c>Jwt:Issuer</c> e <c>Jwt:Audience</c>.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    /// <returns>O próprio construtor, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A chave pública não foi configurada ou não é um PEM RSA válido.</exception>
    public static WebApplicationBuilder AddAutenticacaoJwt(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var secao = builder.Configuration.GetSection("Jwt");
        var pem = secao["ChavePublicaPem"];
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvalidOperationException("Jwt:ChavePublicaPem não foi configurada.");
        }

        var chave = CarregarChavePublica(pem);
        var emissor = secao["Issuer"] ?? "conexao-identity";
        var audiencia = secao["Audience"] ?? "conexao-api";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.MapInboundClaims = false;
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = chave,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ValidateIssuer = true,
                    ValidIssuer = emissor,
                    ValidateAudience = true,
                    ValidAudience = audiencia,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = ClaimRole,
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(PolicyGestorOng, politica => politica.RequireAuthenticatedUser().RequireRole("GestorONG"))
            .AddPolicy(PolicyDoador, politica => politica.RequireAuthenticatedUser().RequireRole("Doador"))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return builder;
    }

    /// <summary>
    /// Carrega a chave pública RSA de um PEM.
    /// </summary>
    /// <param name="pem">Chave pública em PEM.</param>
    /// <returns>Chave de validação da assinatura.</returns>
    private static RsaSecurityKey CarregarChavePublica(string pem)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception excecao) when (excecao is CryptographicException or ArgumentException)
        {
            rsa.Dispose();
            throw new InvalidOperationException("Jwt:ChavePublicaPem não é um PEM RSA válido.");
        }

        return new RsaSecurityKey(rsa);
    }
}
