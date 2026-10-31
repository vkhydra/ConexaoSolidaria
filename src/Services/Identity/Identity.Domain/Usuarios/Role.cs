namespace Identity.Domain.Usuarios;

/// <summary>
/// Papel do usuário na plataforma. O nome do papel é o valor da claim <c>role</c> do token JWT.
/// </summary>
public enum Role
{
    /// <summary>
    /// Pessoa física que faz doações.
    /// </summary>
    Doador = 1,

    /// <summary>
    /// Funcionário da ONG que administra as campanhas.
    /// </summary>
    GestorONG = 2,
}
