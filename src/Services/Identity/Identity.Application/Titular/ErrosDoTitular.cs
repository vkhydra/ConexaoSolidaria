using Identity.Domain.Comum;

namespace Identity.Application.Titular;

/// <summary>
/// Erros comuns aos casos de uso do titular.
/// </summary>
public static class ErrosDoTitular
{
    /// <summary>
    /// Código do erro de usuário não autenticado: o <c>sub</c> do token não existe mais ou foi anonimizado.
    /// </summary>
    public const string CodigoNaoAutenticado = "auth.nao_autenticado";

    /// <summary>
    /// Obtém o erro devolvido quando o token aponta para um usuário inexistente ou anonimizado.
    /// </summary>
    public static Erro NaoAutenticado { get; } = new(CodigoNaoAutenticado, "Autenticação necessária.");
}
