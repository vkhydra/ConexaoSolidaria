namespace Identity.Application.Autenticacao;

/// <summary>
/// Dados do login.
/// </summary>
/// <param name="Email">E-mail digitado, em qualquer caixa.</param>
/// <param name="Senha">Senha em texto digitada no login; nunca é registrada.</param>
public sealed record AutenticarRequest(string? Email, string? Senha);
