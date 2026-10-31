namespace Identity.UnitTests.Fixtures;

/// <summary>
/// Tipo de apoio que depende de <c>System.Net.Http</c> de propósito, para provar que a regra
/// de arquitetura acusa uma dependência proibida.
/// </summary>
public sealed class ClienteProibido
{
    /// <summary>
    /// Obtém ou define o cliente HTTP, dependência usada só pelo teste da regra.
    /// </summary>
    public HttpClient? Cliente { get; set; }
}
