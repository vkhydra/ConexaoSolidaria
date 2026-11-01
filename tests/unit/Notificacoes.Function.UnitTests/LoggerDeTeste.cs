using Microsoft.Extensions.Logging;
using Notificacoes.Function.Recibos;

namespace Notificacoes.Function.UnitTests;

/// <summary>
/// Logger do <see cref="EmissorRecibo"/> que guarda cada registro (nível, nome do evento, mensagem e campos estruturados)
/// para os testes conferirem.
/// </summary>
public sealed class LoggerDeTeste : ILogger<EmissorRecibo>
{
    /// <summary>
    /// Obtém os registros recebidos, na ordem.
    /// </summary>
    public List<RegistroDeLog> Registros { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var campos = state is IEnumerable<KeyValuePair<string, object?>> pares
            ? pares.Where(p => p.Key != "{OriginalFormat}").ToDictionary(p => p.Key, p => p.Value)
            : [];
        Registros.Add(new RegistroDeLog(logLevel, eventId.Name, formatter(state, exception), campos));
    }
}

/// <summary>
/// Um registro de log capturado.
/// </summary>
/// <param name="Nivel">Nível do registro.</param>
/// <param name="NomeDoEvento">Nome do evento de log.</param>
/// <param name="Mensagem">Mensagem já formatada.</param>
/// <param name="Campos">Campos estruturados do registro.</param>
public sealed record RegistroDeLog(LogLevel Nivel, string? NomeDoEvento, string Mensagem, IReadOnlyDictionary<string, object?> Campos);
