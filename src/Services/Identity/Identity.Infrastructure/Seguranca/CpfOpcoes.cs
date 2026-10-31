namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Chaves de proteção do CPF, lidas da seção <c>Cpf</c> (variáveis <c>Cpf__*</c>). Ambas vêm de Secret e nunca vão para log.
/// </summary>
public sealed class CpfOpcoes
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string Secao = "Cpf";

    /// <summary>
    /// Obtém ou define a chave AES-256 da cifra, em Base64 (32 bytes).
    /// </summary>
    public string ChaveCifra { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a chave do HMAC-SHA256 do hash, em Base64 (32 bytes).
    /// </summary>
    public string ChaveHash { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a versão da chave de cifra, gravada no primeiro byte de cada CPF cifrado para permitir rotação.
    /// </summary>
    public byte VersaoChave { get; set; } = 1;
}
