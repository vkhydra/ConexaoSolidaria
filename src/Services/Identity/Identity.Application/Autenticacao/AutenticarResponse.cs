namespace Identity.Application.Autenticacao;

/// <summary>
/// Resposta do login bem-sucedido.
/// </summary>
/// <param name="AccessToken">JWT assinado.</param>
/// <param name="TokenType">Tipo do token, sempre <c>Bearer</c>.</param>
/// <param name="ExpiresIn">Validade do token, em segundos.</param>
/// <param name="Role">Papel do usuário autenticado.</param>
public sealed record AutenticarResponse(string AccessToken, string TokenType, int ExpiresIn, string Role);
