using Serilog.Core;
using Serilog.Events;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;

/// <summary>
/// Destino de log em memória, usado para conferir o que o Serilog emitiu nos testes.
/// </summary>
public sealed class SinkEmMemoria : ILogEventSink
{
    /// <summary>
    /// Obtém os eventos recebidos, na ordem em que chegaram.
    /// </summary>
    public List<LogEvent> Eventos { get; } = [];

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        Eventos.Add(logEvent);
    }
}
