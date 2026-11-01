using Notificacoes.Function.Recibos;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Notificacoes.Function.IntegrationTests;

/// <summary>
/// Testes do <see cref="DeduplicadorRecibosRedis"/> com um Redis real em container: a primeira reserva vence, as seguintes
/// falham, e a chave expira em 7 dias.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DeduplicadorRecibosRedisTests : IAsyncLifetime
{
    /// <summary>
    /// Container do Redis.
    /// </summary>
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

    /// <summary>
    /// Conexão com o Redis do container.
    /// </summary>
    private ConnectionMultiplexer _conexao = null!;

    /// <summary>
    /// Sobe o Redis e conecta.
    /// </summary>
    /// <returns>Tarefa que termina com o Redis pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _conexao = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
    }

    /// <summary>
    /// Libera a conexão e o container.
    /// </summary>
    /// <returns>Tarefa que termina com tudo liberado.</returns>
    public async ValueTask DisposeAsync()
    {
        await _conexao.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Dada a mesma doação entregue duas vezes, quando registrada, então só a primeira vence e a chave expira em 7 dias.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task TentarRegistrar_MesmaDoacaoDuasVezes_SoAPrimeiraVence()
    {
        var ct = TestContext.Current.CancellationToken;
        var deduplicador = new DeduplicadorRecibosRedis(_conexao);
        var doacao = Guid.NewGuid();

        var primeira = await deduplicador.TentarRegistrarAsync(doacao, ct);
        var segunda = await deduplicador.TentarRegistrarAsync(doacao, ct);

        primeira.ShouldBeTrue();
        segunda.ShouldBeFalse();
        var validade = await _conexao.GetDatabase().KeyTimeToLiveAsync(DeduplicadorRecibosRedis.Chave(doacao));
        validade.ShouldNotBeNull();
        validade.Value.ShouldBeGreaterThan(TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1));
        validade.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromDays(7));
    }

    /// <summary>
    /// Dadas 20 entregas simultâneas da mesma doação, quando registradas, então exatamente uma vence.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task TentarRegistrar_EntregasSimultaneas_ExatamenteUmaVence()
    {
        var deduplicador = new DeduplicadorRecibosRedis(_conexao);
        var doacao = Guid.NewGuid();

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => deduplicador.TentarRegistrarAsync(doacao, TestContext.Current.CancellationToken)));

        resultados.Count(r => r).ShouldBe(1);
    }

    /// <summary>
    /// Dadas doações diferentes, quando registradas, então todas vencem (a chave é por doação).
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task TentarRegistrar_DoacoesDiferentes_TodasVencem()
    {
        var deduplicador = new DeduplicadorRecibosRedis(_conexao);

        var a = await deduplicador.TentarRegistrarAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var b = await deduplicador.TentarRegistrarAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        (a && b).ShouldBeTrue();
    }
}
