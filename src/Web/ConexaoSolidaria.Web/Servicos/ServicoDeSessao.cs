using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Abre e encerra a sessão do usuário no servidor.
/// </summary>
public interface IServicoDeSessao
{
    /// <summary>
    /// Grava o cookie de autenticação com o JWT dentro dele, para o navegador nunca ver o token.
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição de login.</param>
    /// <param name="login">Resposta do login, com o JWT e o papel.</param>
    /// <param name="nome">Nome exibido no menu.</param>
    /// <returns>Tarefa que termina com o cookie gravado.</returns>
    Task EntrarAsync(HttpContext contexto, LoginResposta login, string nome);

    /// <summary>
    /// Apaga o cookie de autenticação.
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição.</param>
    /// <returns>Tarefa que termina com a sessão encerrada.</returns>
    Task SairAsync(HttpContext contexto);
}

/// <summary>
/// Sessão por cookie de autenticação do ASP.NET Core. O JWT fica na claim <c>access_token</c>, dentro do cookie protegido,
/// e o cookie vale o mesmo tempo que o token.
/// </summary>
public sealed class ServicoDeSessao : IServicoDeSessao
{
    /// <inheritdoc/>
    public async Task EntrarAsync(HttpContext contexto, LoginResposta login, string nome)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(login);

        var claims = new List<Claim>
        {
            new("name", nome),
            new("role", login.Role),
            new(SessaoDoUsuario.ClaimDoToken, login.AccessToken),
        };
        var sub = LerSub(login.AccessToken);
        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");
        await contexto.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade),
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(login.ExpiresIn), IsPersistent = false });
    }

    /// <inheritdoc/>
    public Task SairAsync(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        return contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Lê a claim <c>sub</c> do JWT sem validar a assinatura (quem valida é a API; aqui só se identifica o usuário na interface).
    /// </summary>
    /// <param name="jwt">JWT recebido da Identity.</param>
    /// <returns>O valor de <c>sub</c>, ou nulo se o token não tem o formato esperado.</returns>
    internal static string? LerSub(string jwt)
    {
        var partes = jwt.Split('.');
        if (partes.Length != 3)
        {
            return null;
        }

        try
        {
            var base64 = partes[1].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            using var documento = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            return documento.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }
        catch (Exception excecao) when (excecao is FormatException or JsonException)
        {
            return null;
        }
    }
}
