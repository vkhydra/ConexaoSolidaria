using Campanhas.Application.Abstracoes;
using Prometheus;

namespace Campanhas.Infrastructure;

/// <summary>
/// Métrica <c>campanhas_criadas_total</c> no Prometheus, sem rótulos.
/// </summary>
public sealed class MetricasCampanhasPrometheus : IMetricasCampanhas
{
    /// <summary>
    /// Contador de campanhas criadas.
    /// </summary>
    private readonly Counter _criadas;

    /// <summary>
    /// Cria o contador no registro padrão do Prometheus.
    /// </summary>
    public MetricasCampanhasPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria o contador na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasCampanhasPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _criadas = fabrica.CreateCounter("campanhas_criadas_total", "Campanhas criadas com sucesso.");
    }

    /// <inheritdoc/>
    public void RegistrarCriacao()
    {
        _criadas.Inc();
    }
}
