using Campanhas.Application.Doacoes;
using Campanhas.Infrastructure.Idempotencia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes do serviço em segundo plano que remove as chaves de idempotência com mais de 24 horas.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LimpezaChavesIdempotenciaTests
{
    /// <summary>
    /// Instante inicial do relógio de teste.
    /// </summary>
    private static readonly DateTimeOffset _inicio = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Porta das chaves de idempotência falsa.
    /// </summary>
    private readonly IChavesIdempotencia _chaves = Substitute.For<IChavesIdempotencia>();

    /// <summary>
    /// Relógio de teste, avançado à mão.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(_inicio);

    /// <summary>
    /// Dado que passou uma hora, quando o ciclo roda, então remove as chaves criadas há mais de 24 horas.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Ciclo_AposUmaHora_RemoveChavesComMaisDe24Horas()
    {
        var chamada = new TaskCompletionSource<(DateTimeOffset Limite, DateTimeOffset Agora)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _chaves.RemoverExpiradasAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(c =>
            {
                chamada.TrySetResult((c.Arg<DateTimeOffset>(), _relogio.GetUtcNow()));
                return Task.FromResult(3);
            });
        using var servico = Criar();

        await servico.StartAsync(TestContext.Current.CancellationToken);
        await AvancarAteAsync(chamada.Task);
        await servico.StopAsync(TestContext.Current.CancellationToken);

        var (limite, agora) = await chamada.Task;
        limite.ShouldBe(agora - TimeSpan.FromHours(24));
        agora.ShouldBeGreaterThanOrEqualTo(_inicio.AddHours(1));
    }

    /// <summary>
    /// Dado que o primeiro ciclo falha, quando a hora seguinte passa, então o serviço continua e tenta de novo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Ciclo_FalhaNoBanco_TentaDeNovoNoProximoCiclo()
    {
        var chamadas = 0;
        var segunda = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _chaves.RemoverExpiradasAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref chamadas) == 1)
                {
                    return Task.FromException<int>(new InvalidOperationException("banco fora do ar"));
                }

                segunda.TrySetResult();
                return Task.FromResult(0);
            });
        using var servico = Criar();

        await servico.StartAsync(TestContext.Current.CancellationToken);
        await AvancarAteAsync(segunda.Task);
        await servico.StopAsync(TestContext.Current.CancellationToken);

        chamadas.ShouldBeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Dado um serviço parado antes da primeira hora, quando ele encerra, então nenhuma limpeza é feita.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Parada_AntesDoPrimeiroCiclo_NaoLimpa()
    {
        using var servico = Criar();

        await servico.StartAsync(TestContext.Current.CancellationToken);
        await servico.StopAsync(TestContext.Current.CancellationToken);

        await _chaves.DidNotReceive().RemoverExpiradasAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Avança o relógio de hora em hora até a tarefa terminar, sem depender de o serviço já ter criado o temporizador.
    /// </summary>
    /// <param name="alvo">Tarefa que termina quando o ciclo esperado acontece.</param>
    /// <returns>Tarefa que termina quando o alvo termina.</returns>
    private async Task AvancarAteAsync(Task alvo)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (!alvo.IsCompleted && DateTime.UtcNow < limite)
        {
            _relogio.Advance(TimeSpan.FromHours(1));
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        await alvo.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Monta o serviço com a porta falsa registrada em um contêiner de serviços real.
    /// </summary>
    /// <returns>O serviço, ainda parado.</returns>
    private LimpezaChavesIdempotenciaHostedService Criar()
    {
        var provedor = new ServiceCollection().AddSingleton(_chaves).BuildServiceProvider();
        return new LimpezaChavesIdempotenciaHostedService(provedor.GetRequiredService<IServiceScopeFactory>(), _relogio, NullLogger<LimpezaChavesIdempotenciaHostedService>.Instance);
    }
}
