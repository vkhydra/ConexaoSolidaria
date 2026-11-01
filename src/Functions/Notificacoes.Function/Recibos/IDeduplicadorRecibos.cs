namespace Notificacoes.Function.Recibos;

/// <summary>
/// Porta que evita recibos repetidos quando a fila entrega a mesma doação mais de uma vez (entrega at-least-once). A função não
/// tem banco próprio; a implementação usa o Redis.
/// </summary>
public interface IDeduplicadorRecibos
{
    /// <summary>
    /// Tenta reservar o recibo da doação. A reserva é atômica: de várias chamadas simultâneas com o mesmo identificador, só uma vence.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Verdadeiro se este é o primeiro recibo da doação; falso se ela já foi registrada antes.</returns>
    Task<bool> TentarRegistrarAsync(Guid doacaoId, CancellationToken cancelamento);
}
