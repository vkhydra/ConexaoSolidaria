using Campanhas.Application.Transparencia;
using Prometheus;

namespace Campanhas.Infrastructure.Cache;

/// <summary>
/// Métrica <c>painel_cache_total{resultado}</c> no Prometheus. O rótulo tem só três valores: acerto, falta e erro.
/// </summary>
public sealed class MetricasPainelPrometheus : IMetricasPainel
{
    /// <summary>
    /// Contador de consultas ao cache por resultado.
    /// </summary>
    private readonly Counter _consultas;

    /// <summary>
    /// Cria o contador no registro padrão do Prometheus.
    /// </summary>
    public MetricasPainelPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria o contador na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasPainelPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _consultas = fabrica.CreateCounter(
            "painel_cache_total",
            "Consultas ao cache do painel por resultado.",
            new CounterConfiguration { LabelNames = ["resultado"] });
    }

    /// <inheritdoc/>
    public void Registrar(ResultadoCache resultado)
    {
        _consultas.WithLabels(resultado.ToString().ToLowerInvariant()).Inc();
    }
}
