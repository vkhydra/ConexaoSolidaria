using Campanhas.Application.Transparencia;
using Microsoft.Extensions.Caching.Hybrid;

namespace Campanhas.Infrastructure.Cache;

/// <summary>
/// Cache do painel com <see cref="HybridCache"/>: memória local (L1) na frente do Redis (L2), com proteção contra avalanche
/// de consultas ao banco quando a chave expira. A chave é versionada para que uma mudança de formato não leia dados antigos.
/// </summary>
/// <param name="cache">Cache híbrido do .NET.</param>
/// <param name="metricas">Métrica de consultas ao cache.</param>
public sealed class CachePainelHybrid(HybridCache cache, IMetricasPainel metricas) : ICachePainel
{
    /// <summary>
    /// Chave do painel no cache; o sufixo <c>v1</c> muda quando o formato de <see cref="PainelItemResponse"/> muda.
    /// </summary>
    public const string Chave = "painel:v1";

    /// <summary>
    /// Validade do painel no cache, a mesma do cabeçalho <c>Cache-Control</c> da API.
    /// </summary>
    public static readonly TimeSpan Validade = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Opções de validade: dez segundos no Redis e cinco na memória local, então o painel nunca fica velho por mais de dez.
    /// </summary>
    private static readonly HybridCacheEntryOptions _opcoes = new()
    {
        Expiration = Validade,
        LocalCacheExpiration = TimeSpan.FromSeconds(5),
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PainelItemResponse>> ObterOuCriarAsync(
        Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>> fabrica,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        var consultouOBanco = false;
        var bancoFalhou = false;
        try
        {
            var painel = await cache.GetOrCreateAsync<PainelItemResponse[]>(
                Chave,
                async ct =>
                {
                    consultouOBanco = true;
                    try
                    {
                        return [.. await fabrica(ct)];
                    }
                    catch (Exception)
                    {
                        bancoFalhou = true;
                        throw;
                    }
                },
                _opcoes,
                cancellationToken: cancelamento);

            metricas.Registrar(consultouOBanco ? ResultadoCache.Falta : ResultadoCache.Acerto);
            return painel;
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException && !bancoFalhou)
        {
            metricas.Registrar(ResultadoCache.Erro);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoverAsync(CancellationToken cancelamento)
    {
        await cache.RemoveAsync(Chave, cancelamento);
    }
}
