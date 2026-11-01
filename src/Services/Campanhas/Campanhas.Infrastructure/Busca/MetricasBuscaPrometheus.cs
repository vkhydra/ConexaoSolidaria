using Campanhas.Application.Busca;
using Prometheus;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Métricas da busca no Prometheus: <c>buscas_total{resultado="ok|vazio|indisponivel"}</c> e <c>campanhas_indexadas_total</c>.
/// </summary>
public sealed class MetricasBuscaPrometheus : IMetricasBusca
{
    /// <summary>
    /// Contador de buscas, por resultado.
    /// </summary>
    private readonly Counter _buscas;

    /// <summary>
    /// Contador de campanhas gravadas no índice.
    /// </summary>
    private readonly Counter _indexadas;

    /// <summary>
    /// Cria os contadores no registro padrão do Prometheus.
    /// </summary>
    public MetricasBuscaPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria os contadores na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasBuscaPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _buscas = fabrica.CreateCounter("buscas_total", "Buscas de campanhas, por resultado.", new CounterConfiguration { LabelNames = ["resultado"] });
        _indexadas = fabrica.CreateCounter("campanhas_indexadas_total", "Campanhas gravadas no índice de busca.");
    }

    /// <inheritdoc/>
    public void RegistrarBusca(ResultadoDaBusca resultado)
    {
        _buscas.WithLabels(resultado switch
        {
            ResultadoDaBusca.Ok => "ok",
            ResultadoDaBusca.Vazio => "vazio",
            _ => "indisponivel",
        }).Inc();
    }

    /// <inheritdoc/>
    public void RegistrarIndexada()
    {
        _indexadas.Inc();
    }
}
