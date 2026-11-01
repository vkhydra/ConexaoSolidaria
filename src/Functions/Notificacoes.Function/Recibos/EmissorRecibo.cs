using System.Diagnostics;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.Logging;

namespace Notificacoes.Function.Recibos;

/// <summary>
/// Emite o recibo de uma doação processada. O recibo é um log estruturado de negócio (<c>ReciboEmitido</c>) com só
/// identificadores e o valor: nunca nome, e-mail ou CPF. Doações repetidas não geram um segundo recibo.
/// </summary>
/// <param name="deduplicador">Evita recibos repetidos.</param>
/// <param name="log">Logger do emissor.</param>
public sealed partial class EmissorRecibo(IDeduplicadorRecibos deduplicador, ILogger<EmissorRecibo> log)
{
    /// <summary>
    /// Emite o recibo, se a doação ainda não teve um.
    /// </summary>
    /// <param name="doacao">Doação processada.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Verdadeiro se o recibo foi emitido; falso se a doação já tinha recibo.</returns>
    public async Task<bool> EmitirAsync(DoacaoProcessadaEvent doacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(doacao);

        if (!await deduplicador.TentarRegistrarAsync(doacao.DoacaoId, cancelamento))
        {
            LogReciboDuplicado(log, doacao.DoacaoId);
            return false;
        }

        var traceId = Activity.Current?.TraceId.ToString();
        LogReciboEmitido(log, doacao.DoacaoId, doacao.CampanhaId, doacao.DoadorId, doacao.Valor, traceId);
        return true;
    }

    /// <summary>
    /// Registra o recibo emitido, com os campos permitidos.
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="campanhaId">Campanha beneficiada.</param>
    /// <param name="doadorId">Referência pseudônima ao doador.</param>
    /// <param name="valor">Valor doado.</param>
    /// <param name="traceId">Identificador do trace, quando há.</param>
    [LoggerMessage(EventId = 1, EventName = "ReciboEmitido", Level = LogLevel.Information, Message = "ReciboEmitido para a doação {DoacaoId} da campanha {CampanhaId} no valor de {Valor} (doador {DoadorId}, trace {TraceId})")]
    private static partial void LogReciboEmitido(ILogger log, Guid doacaoId, Guid campanhaId, Guid doadorId, decimal valor, string? traceId);

    /// <summary>
    /// Registra que a doação já tinha recibo.
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    [LoggerMessage(EventId = 2, EventName = "ReciboDuplicadoIgnorado", Level = LogLevel.Debug, Message = "ReciboDuplicadoIgnorado para a doação {DoacaoId}")]
    private static partial void LogReciboDuplicado(ILogger log, Guid doacaoId);
}
