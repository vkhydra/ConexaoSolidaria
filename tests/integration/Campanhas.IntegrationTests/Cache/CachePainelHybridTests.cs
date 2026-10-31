using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Campanhas.IntegrationTests.Cache;

/// <summary>
/// Testes do cache do painel contra um Redis real (Testcontainers): acerto, falta, remoção e compartilhamento entre instâncias.
/// </summary>
public sealed class CachePainelHybridTests : IAsyncLifetime
{
    /// <summary>
    /// Container do Redis.
    /// </summary>
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    /// <summary>
    /// Métrica de teste que guarda os resultados registrados, no lugar da métrica global do Prometheus.
    /// </summary>
    private readonly IMetricasPainel _metricas = Substitute.For<IMetricasPainel>();

    /// <summary>
    /// Sobe o Redis.
    /// </summary>
    /// <returns>Tarefa que termina com o Redis pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();
    }

    /// <summary>
    /// Remove o Redis.
    /// </summary>
    /// <returns>Tarefa que termina com o container removido.</returns>
    public async ValueTask DisposeAsync()
    {
        await _redis.DisposeAsync();
    }

    /// <summary>
    /// Dado um painel já em cache, quando consultado de novo, então a fábrica não é chamada; depois de removido, é chamada outra vez.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ObterOuCriar_AcertoFaltaERemocao_ChamaAFabricaSoQuandoPreciso()
    {
        await using var provedor = CriarProvedor();
        var cache = provedor.GetRequiredService<ICachePainel>();
        var chamadas = 0;
        Task<IReadOnlyList<PainelItemResponse>> Fabrica(CancellationToken ct)
        {
            chamadas++;
            return Task.FromResult<IReadOnlyList<PainelItemResponse>>([Item($"chamada {chamadas}")]);
        }

        var primeira = await cache.ObterOuCriarAsync(Fabrica, CancellationToken.None);
        var segunda = await cache.ObterOuCriarAsync(Fabrica, CancellationToken.None);
        await cache.RemoverAsync(CancellationToken.None);
        var terceira = await cache.ObterOuCriarAsync(Fabrica, CancellationToken.None);

        primeira.ShouldHaveSingleItem().Titulo.ShouldBe("chamada 1");
        segunda.ShouldHaveSingleItem().Titulo.ShouldBe("chamada 1");
        terceira.ShouldHaveSingleItem().Titulo.ShouldBe("chamada 2");
        chamadas.ShouldBe(2);
        _metricas.Received(2).Registrar(ResultadoCache.Falta);
        _metricas.Received(1).Registrar(ResultadoCache.Acerto);
    }

    /// <summary>
    /// Dadas duas instâncias da API com o mesmo Redis, quando uma guarda o painel, então a outra o lê sem consultar o banco.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ObterOuCriar_DuasInstancias_CompartilhamOPainelPeloRedis()
    {
        await using var instanciaA = CriarProvedor();
        await using var instanciaB = CriarProvedor();
        var painel = new PainelItemResponse(Guid.NewGuid(), "Compartilhada", 100m, 10m, 10m, new DateOnly(2026, 12, 24));
        await instanciaA.GetRequiredService<ICachePainel>().ObterOuCriarAsync(_ => Task.FromResult<IReadOnlyList<PainelItemResponse>>([painel]), CancellationToken.None);

        var lido = await instanciaB.GetRequiredService<ICachePainel>().ObterOuCriarAsync(
            _ => throw new InvalidOperationException("A instância B não deveria consultar o banco."),
            CancellationToken.None);

        lido.ShouldHaveSingleItem().ShouldBe(painel);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando o cache é consultado, então a chamada é recusada; sem Redis configurado, o registro falha.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Argumentos_NulosOuSemRedis_Recusam()
    {
        await using var provedor = CriarProvedor();

        await Should.ThrowAsync<ArgumentNullException>(() => provedor.GetRequiredService<ICachePainel>().ObterOuCriarAsync(null!, CancellationToken.None));
        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddCampanhasCache(new ConfigurationBuilder().Build()));
        Should.Throw<ArgumentNullException>(() => ExtensoesCampanhasInfraestrutura.AddCampanhasCache(null!, new ConfigurationBuilder().Build()));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddCampanhasCache(null!));
    }

    /// <summary>
    /// Monta um provedor de serviços com o cache do painel apontando para o Redis do container.
    /// </summary>
    /// <returns>O provedor, que o chamador deve descartar.</returns>
    private ServiceProvider CriarProvedor()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Redis"] = _redis.GetConnectionString() })
            .Build();
        return new ServiceCollection().AddLogging().AddCampanhasCache(configuracao).AddSingleton<IMetricasPainel>(_metricas).BuildServiceProvider();
    }

    /// <summary>
    /// Cria um item de painel para os testes.
    /// </summary>
    /// <param name="titulo">Título do item.</param>
    /// <returns>O item.</returns>
    private static PainelItemResponse Item(string titulo)
    {
        return new PainelItemResponse(Guid.NewGuid(), titulo, 100m, 0m, 0m, new DateOnly(2026, 12, 24));
    }
}
