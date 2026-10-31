using Campanhas.Application.Doacoes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Campanhas.Infrastructure.Idempotencia;

/// <summary>
/// Serviço em segundo plano que, a cada hora, remove as chaves de idempotência com mais de 24 horas. Uma falha de ciclo
/// é registrada e o serviço tenta de novo no ciclo seguinte.
/// </summary>
/// <param name="escopos">Fábrica de escopos, porque as chaves vivem em um contexto de banco com escopo.</param>
/// <param name="relogio">Relógio, trocável nos testes.</param>
/// <param name="log">Logger do serviço.</param>
public sealed partial class LimpezaChavesIdempotenciaHostedService(IServiceScopeFactory escopos, TimeProvider relogio, ILogger<LimpezaChavesIdempotenciaHostedService> log) : BackgroundService
{
    /// <summary>
    /// Intervalo entre as limpezas.
    /// </summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    /// <summary>
    /// Tempo de vida de uma chave de idempotência.
    /// </summary>
    public static readonly TimeSpan Validade = TimeSpan.FromHours(24);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo, relogio);
        try
        {
            while (true)
            {
                // Só devolve falso se o temporizador for descartado, o que aqui acontece depois do laço; a parada do host é por cancelamento.
                await temporizador.WaitForNextTickAsync(stoppingToken);
                await LimparAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal do host.
        }
    }

    /// <summary>
    /// Executa um ciclo de limpeza; falhas são registradas e não derrubam o serviço.
    /// </summary>
    /// <param name="cancelamento">Token de parada do host.</param>
    /// <returns>Tarefa que termina com o ciclo concluído.</returns>
    private async Task LimparAsync(CancellationToken cancelamento)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var chaves = escopo.ServiceProvider.GetRequiredService<IChavesIdempotencia>();
            var removidas = await chaves.RemoverExpiradasAsync(relogio.GetUtcNow() - Validade, cancelamento);
            LogLimpeza(log, removidas);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            LogFalha(log, excecao);
        }
    }

    /// <summary>
    /// Registra a quantidade de chaves removidas no ciclo.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    /// <param name="removidas">Quantidade de chaves removidas.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Limpeza de chaves de idempotência: {Removidas} removida(s).")]
    private static partial void LogLimpeza(ILogger log, int removidas);

    /// <summary>
    /// Registra a falha de um ciclo de limpeza.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    /// <param name="excecao">Exceção ocorrida.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Falha na limpeza das chaves de idempotência; nova tentativa no próximo ciclo.")]
    private static partial void LogFalha(ILogger log, Exception excecao);
}
