using Microsoft.Extensions.Logging;

namespace Identity.UnitTests.Suporte;

/// <summary>
/// Logger de teste que guarda nível e texto de cada mensagem, para conferir o que é registrado.
/// </summary>
/// <typeparam name="T">Tipo dono do logger.</typeparam>
public sealed class LoggerCaptura<T> : ILogger<T>
{
    /// <summary>
    /// Obtém os níveis registrados.
    /// </summary>
    public List<LogLevel> Niveis { get; } = [];

    /// <summary>
    /// Obtém os textos registrados.
    /// </summary>
    public List<string> Mensagens { get; } = [];

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
        Niveis.Add(logLevel);
        Mensagens.Add(formatter(state, exception));
    }
}
