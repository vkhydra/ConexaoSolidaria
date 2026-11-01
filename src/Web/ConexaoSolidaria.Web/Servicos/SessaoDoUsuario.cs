using Microsoft.AspNetCore.Components.Authorization;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Acesso ao JWT da sessão do usuário atual.
/// </summary>
public interface ISessaoDoUsuario
{
    /// <summary>
    /// Obtém o JWT guardado no cookie de autenticação do servidor.
    /// </summary>
    /// <returns>O JWT, ou nulo se não há usuário autenticado.</returns>
    Task<string?> ObterTokenAsync();
}

/// <summary>
/// Lê o JWT da claim <c>access_token</c> do usuário autenticado. O token fica só no servidor, dentro do cookie de
/// autenticação; nunca vai para o navegador em outro lugar.
/// </summary>
/// <param name="autenticacao">Provedor do estado de autenticação do circuito ou da requisição.</param>
public sealed class SessaoDoUsuario(AuthenticationStateProvider autenticacao) : ISessaoDoUsuario
{
    /// <summary>
    /// Nome da claim que guarda o JWT.
    /// </summary>
    public const string ClaimDoToken = "access_token";

    /// <inheritdoc/>
    public async Task<string?> ObterTokenAsync()
    {
        var estado = await autenticacao.GetAuthenticationStateAsync();
        return estado.User.FindFirst(ClaimDoToken)?.Value;
    }
}
