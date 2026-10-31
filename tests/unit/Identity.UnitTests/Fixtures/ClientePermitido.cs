namespace Identity.UnitTests.Fixtures;

/// <summary>
/// Tipo de apoio sem dependências proibidas, para provar que a regra de arquitetura
/// aceita um tipo correto.
/// </summary>
public sealed class ClientePermitido
{
    /// <summary>
    /// Obtém ou define um nome qualquer, sem relação com HTTP.
    /// </summary>
    public string? Nome { get; set; }
}
