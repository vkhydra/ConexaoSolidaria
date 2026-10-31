using Microsoft.Extensions.Logging;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;

/// <summary>
/// Provedor de log do <c>Microsoft.Extensions.Logging</c> que guarda as mensagens em memória,
/// para conferir o que um componente registrou.
/// </summary>
public sealed class ProvedorLogEmMemoria : ILoggerProvider
{
    /// <summary>
    /// Obtém as entradas registradas, com o nível e a mensagem renderizada.
    /// </summary>
    public List<(LogLevel Nivel, string Mensagem)> Entradas { get; } = [];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new RegistradorEmMemoria(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nada a liberar: as entradas ficam em memória até o fim do teste.
    }

    /// <summary>
    /// Registrador que repassa cada mensagem ao provedor.
    /// </summary>
    private sealed class RegistradorEmMemoria : ILogger
    {
        /// <summary>
        /// Provedor dono das entradas.
        /// </summary>
        private readonly ProvedorLogEmMemoria _provedor;

        /// <summary>
        /// Cria o registrador.
        /// </summary>
        /// <param name="provedor">Provedor que recebe as entradas.</param>
        public RegistradorEmMemoria(ProvedorLogEmMemoria provedor)
        {
            _provedor = provedor;
        }

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
            _provedor.Entradas.Add((logLevel, formatter(state, exception)));
        }
    }
}
