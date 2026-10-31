namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Configuração do token JWT, lida da seção <c>Jwt</c> (variáveis <c>Jwt__*</c>).
/// </summary>
public sealed class JwtOpcoes
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string Secao = "Jwt";

    /// <summary>
    /// Obtém ou define o emissor (<c>iss</c>) do token.
    /// </summary>
    public string Issuer { get; set; } = "conexao-identity";

    /// <summary>
    /// Obtém ou define a audiência (<c>aud</c>) do token.
    /// </summary>
    public string Audience { get; set; } = "conexao-api";

    /// <summary>
    /// Obtém ou define a chave privada RSA em PEM; vem de Secret e nunca é registrada em log.
    /// </summary>
    public string ChavePrivadaPem { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a validade do token, em segundos (60 minutos).
    /// </summary>
    public int ValidadeEmSegundos { get; set; } = 3600;
}
