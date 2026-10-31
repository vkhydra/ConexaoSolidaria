using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ConexaoSolidaria.ServiceDefaults.Logs;

/// <summary>
/// Acrescenta <c>traceId</c> e <c>spanId</c> da atividade atual em cada linha de log,
/// para ligar o log ao trace distribuído (OpenTelemetry).
/// </summary>
public sealed class EnriquecedorTrace : ILogEventEnricher
{
    /// <summary>
    /// Nome da propriedade com o identificador do trace.
    /// </summary>
    public const string PropriedadeTraceId = "traceId";

    /// <summary>
    /// Nome da propriedade com o identificador do span.
    /// </summary>
    public const string PropriedadeSpanId = "spanId";

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var atividade = Activity.Current;
        if (atividade is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropriedadeTraceId, atividade.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropriedadeSpanId, atividade.SpanId.ToString()));
    }
}
