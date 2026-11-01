using StackExchange.Redis;

namespace Notificacoes.Function.Recibos;

/// <summary>
/// Deduplica recibos no Redis com <c>SET conexao:recibos:v1:&lt;doacaoId&gt; 1 NX EX 604800</c>: a operação é atômica, então de
/// duas entregas simultâneas da mesma doação só uma emite o recibo. Sete dias cobrem qualquer reentrega.
/// </summary>
/// <param name="redis">Conexão com o Redis.</param>
public sealed class DeduplicadorRecibosRedis(IConnectionMultiplexer redis) : IDeduplicadorRecibos
{
    /// <summary>
    /// Tempo que a reserva do recibo permanece no Redis.
    /// </summary>
    public static readonly TimeSpan Validade = TimeSpan.FromDays(7);

    /// <summary>
    /// Monta a chave de uma doação.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <returns>A chave no formato <c>conexao:recibos:v1:&lt;doacaoId&gt;</c>.</returns>
    public static string Chave(Guid doacaoId)
    {
        return $"conexao:recibos:v1:{doacaoId:D}";
    }

    /// <inheritdoc/>
    public async Task<bool> TentarRegistrarAsync(Guid doacaoId, CancellationToken cancelamento)
    {
        cancelamento.ThrowIfCancellationRequested();

        return await redis.GetDatabase().StringSetAsync(Chave(doacaoId), "1", Validade, When.NotExists);
    }
}
