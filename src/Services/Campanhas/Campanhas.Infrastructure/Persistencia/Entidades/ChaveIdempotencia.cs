namespace Campanhas.Infrastructure.Persistencia.Entidades;

/// <summary>
/// Chave de idempotência enviada no cabeçalho <c>Idempotency-Key</c> do recebimento de doação. Repetir a chave com o mesmo
/// corpo devolve a mesma doação; com outro corpo, é conflito. Os registros expiram em 24 horas.
/// </summary>
public sealed class ChaveIdempotencia
{
    /// <summary>
    /// Obtém ou define o valor da chave, de até 100 caracteres; junto com o doador forma a chave primária.
    /// </summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o doador dono da chave; chaves iguais de doadores diferentes não se misturam.
    /// </summary>
    public Guid DoadorId { get; set; }

    /// <summary>
    /// Obtém ou define a doação gerada na primeira requisição com a chave.
    /// </summary>
    public Guid DoacaoId { get; set; }

    /// <summary>
    /// Obtém ou define o hash SHA-256 do corpo da primeira requisição, para detectar a chave reusada com outro corpo.
    /// </summary>
    public byte[] HashCorpo { get; set; } = [];

    /// <summary>
    /// Obtém ou define o instante em que a chave foi registrada.
    /// </summary>
    public DateTimeOffset CriadaEm { get; set; }
}
