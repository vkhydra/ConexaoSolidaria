using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Transparencia;

/// <summary>
/// Remove o painel do cache depois que uma campanha é criada ou alterada, para que a mudança apareça sem esperar a validade.
/// Falha ao remover não derruba o caso de uso: o cache é uma otimização e a validade de dez segundos corrige o resto.
/// </summary>
/// <param name="cache">Cache do painel.</param>
/// <param name="log">Logger do invalidador.</param>
public sealed partial class InvalidadorDoPainel(ICachePainel cache, ILogger<InvalidadorDoPainel> log)
{
    /// <summary>
    /// Remove o painel do cache.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando a remoção foi tentada.</returns>
    public async Task InvalidarAsync(CancellationToken cancelamento)
    {
        try
        {
            await cache.RemoverAsync(cancelamento);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            LogFalhaNaInvalidacao(log, excecao.GetType().Name);
        }
    }

    /// <summary>
    /// Registra que o painel não pôde ser removido do cache, só com o tipo da exceção.
    /// </summary>
    /// <param name="log">Logger do invalidador.</param>
    /// <param name="tipoDaExcecao">Nome do tipo da exceção.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível invalidar o painel no cache ({TipoDaExcecao}); ele expira sozinho em instantes.")]
    private static partial void LogFalhaNaInvalidacao(ILogger log, string tipoDaExcecao);
}
