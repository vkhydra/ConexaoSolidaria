namespace Identity.Domain.Usuarios;

/// <summary>
/// Identificador de um usuário (GUID versão 7, ordenável por data de criação). Vai na claim <c>sub</c> do token.
/// </summary>
/// <param name="Valor">GUID do usuário.</param>
public readonly record struct UsuarioId(Guid Valor)
{
    /// <summary>
    /// Gera um identificador novo.
    /// </summary>
    /// <returns>Identificador único e ordenável no tempo.</returns>
    public static UsuarioId Novo()
    {
        return new UsuarioId(Guid.CreateVersion7());
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Valor.ToString();
    }
}
