using Identity.Application.Abstracoes;
using Prometheus;

namespace Identity.Infrastructure;

/// <summary>
/// Métrica <c>logins_total{resultado}</c> no Prometheus. O rótulo tem só três valores, de baixa cardinalidade.
/// </summary>
public sealed class MetricasLoginPrometheus : IMetricasLogin
{
    /// <summary>
    /// Contador de tentativas de login por resultado.
    /// </summary>
    private readonly Counter _logins;

    /// <summary>
    /// Cria o contador no registro padrão do Prometheus.
    /// </summary>
    public MetricasLoginPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria o contador na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasLoginPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _logins = fabrica.CreateCounter(
            "logins_total",
            "Tentativas de login por resultado.",
            new CounterConfiguration { LabelNames = ["resultado"] });
    }

    /// <inheritdoc/>
    public void Registrar(ResultadoLogin resultado)
    {
        _logins.WithLabels(resultado.ToString().ToLowerInvariant()).Inc();
    }
}
