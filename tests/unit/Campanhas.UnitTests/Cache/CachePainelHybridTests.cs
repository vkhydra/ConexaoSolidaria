using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Prometheus;

namespace Campanhas.UnitTests.Cache;

/// <summary>
/// Testes do cache do painel e da métrica <c>painel_cache_total</c> com um <see cref="HybridCache"/> falso, sem Redis.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CachePainelHybridTests
{
    /// <summary>
    /// Métrica falsa.
    /// </summary>
    private readonly IMetricasPainel _metricas = Substitute.For<IMetricasPainel>();

    /// <summary>
    /// Dado um cache que já tem o painel, quando consultado, então conta acerto e não chama a fábrica.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterOuCriar_PainelEmCache_ContaAcerto()
    {
        var cache = new CachePainelHybrid(new HybridCacheFalso(valorEmCache: [Item()]), _metricas);

        var painel = await cache.ObterOuCriarAsync(_ => throw new InvalidOperationException("não deveria consultar o banco"), CancellationToken.None);

        painel.Count.ShouldBe(1);
        _metricas.Received(1).Registrar(ResultadoCache.Acerto);
    }

    /// <summary>
    /// Dado um cache vazio, quando consultado, então chama a fábrica e conta falta.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterOuCriar_CacheVazio_ChamaAFabricaEContaFalta()
    {
        var cache = new CachePainelHybrid(new HybridCacheFalso(), _metricas);

        var painel = await cache.ObterOuCriarAsync(_ => Task.FromResult<IReadOnlyList<PainelItemResponse>>([Item()]), CancellationToken.None);

        painel.Count.ShouldBe(1);
        _metricas.Received(1).Registrar(ResultadoCache.Falta);
    }

    /// <summary>
    /// Dado um cache que falha (Redis fora do ar), quando consultado, então conta erro e a falha sobe para o caso de uso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterOuCriar_CacheFalha_ContaErroEPropaga()
    {
        var cache = new CachePainelHybrid(new HybridCacheFalso(falhaDoCache: new InvalidOperationException("redis")), _metricas);

        await Should.ThrowAsync<InvalidOperationException>(() => cache.ObterOuCriarAsync(_ => Task.FromResult<IReadOnlyList<PainelItemResponse>>([]), CancellationToken.None));

        _metricas.Received(1).Registrar(ResultadoCache.Erro);
    }

    /// <summary>
    /// Dado um banco que falha dentro da fábrica, quando consultado, então a falha sobe sem contar erro de cache.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterOuCriar_BancoFalha_PropagaSemContarErroDeCache()
    {
        var cache = new CachePainelHybrid(new HybridCacheFalso(), _metricas);

        await Should.ThrowAsync<TimeoutException>(() => cache.ObterOuCriarAsync(_ => throw new TimeoutException("banco"), CancellationToken.None));

        _metricas.DidNotReceive().Registrar(Arg.Any<ResultadoCache>());
    }

    /// <summary>
    /// Dado um cancelamento, quando o cache é consultado, então o cancelamento sobe sem contar erro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterOuCriar_Cancelado_NaoContaErro()
    {
        var cache = new CachePainelHybrid(new HybridCacheFalso(falhaDoCache: new OperationCanceledException()), _metricas);

        await Should.ThrowAsync<OperationCanceledException>(() => cache.ObterOuCriarAsync(_ => Task.FromResult<IReadOnlyList<PainelItemResponse>>([]), CancellationToken.None));

        _metricas.DidNotReceive().Registrar(Arg.Any<ResultadoCache>());
    }

    /// <summary>
    /// Dado o painel em cache, quando removido, então a chave versionada é removida; fábrica nula é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Remover_RemoveAChaveVersionada()
    {
        var falso = new HybridCacheFalso();
        var cache = new CachePainelHybrid(falso, _metricas);

        await cache.RemoverAsync(CancellationToken.None);

        falso.ChavesRemovidas.ShouldBe([CachePainelHybrid.Chave]);
        CachePainelHybrid.Chave.ShouldBe("painel:v1");
        await Should.ThrowAsync<ArgumentNullException>(() => cache.ObterOuCriarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Dadas consultas de resultados diferentes, quando registradas, então cada rótulo conta separado.
    /// </summary>
    [Fact]
    public void MetricasPainelPrometheus_Registrar_ContaPorRotulo()
    {
        var fabrica = Metrics.WithCustomRegistry(Metrics.NewCustomRegistry());
        var metricas = new MetricasPainelPrometheus(fabrica);

        metricas.Registrar(ResultadoCache.Acerto);
        metricas.Registrar(ResultadoCache.Acerto);
        metricas.Registrar(ResultadoCache.Falta);
        metricas.Registrar(ResultadoCache.Erro);

        var contador = fabrica.CreateCounter("painel_cache_total", "Consultas ao cache do painel por resultado.", new CounterConfiguration { LabelNames = ["resultado"] });
        contador.WithLabels("acerto").Value.ShouldBe(2);
        contador.WithLabels("falta").Value.ShouldBe(1);
        contador.WithLabels("erro").Value.ShouldBe(1);
        Should.Throw<ArgumentNullException>(() => new MetricasPainelPrometheus(null!));
    }

    /// <summary>
    /// Cria um item de painel para os testes.
    /// </summary>
    /// <returns>O item.</returns>
    private static PainelItemResponse Item()
    {
        return new PainelItemResponse(Guid.NewGuid(), "A", 100m, 10m, 10m, new DateOnly(2026, 12, 24));
    }

    /// <summary>
    /// <see cref="HybridCache"/> falso: devolve um valor pronto, chama a fábrica ou falha, conforme configurado.
    /// </summary>
    private sealed class HybridCacheFalso : HybridCache
    {
        /// <summary>
        /// Valor que o cache já tem; nulo significa cache vazio.
        /// </summary>
        private readonly PainelItemResponse[]? _valorEmCache;

        /// <summary>
        /// Exceção que o cache lança ao ser consultado; nula significa funcionamento normal.
        /// </summary>
        private readonly Exception? _falhaDoCache;

        /// <summary>
        /// Cria o cache falso.
        /// </summary>
        /// <param name="valorEmCache">Valor já em cache, ou nulo.</param>
        /// <param name="falhaDoCache">Exceção a lançar na consulta, ou nula.</param>
        public HybridCacheFalso(PainelItemResponse[]? valorEmCache = null, Exception? falhaDoCache = null)
        {
            _valorEmCache = valorEmCache;
            _falhaDoCache = falhaDoCache;
        }

        /// <summary>
        /// Obtém as chaves removidas.
        /// </summary>
        public List<string> ChavesRemovidas { get; } = [];

        /// <inheritdoc/>
        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_falhaDoCache is not null)
            {
                throw _falhaDoCache;
            }

            if (_valorEmCache is not null)
            {
                return (T)(object)_valorEmCache;
            }

            return await factory(state, cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            ChavesRemovidas.Add(key);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
