namespace Campanhas.Application.Doacoes;

/// <summary>
/// Chave de idempotência já registrada: a doação criada na primeira requisição e o hash do corpo que a originou.
/// </summary>
/// <param name="DoacaoId">Doação criada na primeira requisição com a chave.</param>
/// <param name="HashCorpo">SHA-256 do corpo da primeira requisição.</param>
public sealed record ChaveRegistrada(Guid DoacaoId, byte[] HashCorpo);

/// <summary>
/// Chave de idempotência a gravar junto com a doação aceita, na mesma transação do Outbox.
/// </summary>
/// <param name="Chave">Valor do cabeçalho <c>Idempotency-Key</c>, de 1 a 100 caracteres.</param>
/// <param name="HashCorpo">SHA-256 do corpo da requisição.</param>
public sealed record ChaveParaRegistrar(string Chave, byte[] HashCorpo);

/// <summary>
/// Porta de consulta e de limpeza das chaves de idempotência das doações. A gravação acontece em
/// <see cref="IPublicadorDeDoacoes"/>, para entrar na mesma transação do Outbox.
/// </summary>
public interface IChavesIdempotencia
{
    /// <summary>
    /// Busca a chave de um doador.
    /// </summary>
    /// <param name="chave">Valor do cabeçalho <c>Idempotency-Key</c>.</param>
    /// <param name="doadorId">Doador dono da chave.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A chave registrada ou nulo se ela não existe (ou já expirou e foi removida).</returns>
    Task<ChaveRegistrada?> ObterAsync(string chave, Guid doadorId, CancellationToken cancelamento);

    /// <summary>
    /// Remove as chaves registradas antes de um instante.
    /// </summary>
    /// <param name="limite">Chaves criadas antes deste instante são removidas.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A quantidade de chaves removidas.</returns>
    Task<int> RemoverExpiradasAsync(DateTimeOffset limite, CancellationToken cancelamento);
}
