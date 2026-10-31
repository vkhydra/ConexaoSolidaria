using Identity.Application.Abstracoes;
using Prometheus;

namespace Identity.Infrastructure;

/// <summary>
/// Métricas <c>doadores_cadastrados_total</c> e <c>titulares_anonimizados_total</c> no Prometheus, sem rótulos.
/// </summary>
public sealed class MetricasDoadoresPrometheus : IMetricasDoadores
{
    /// <summary>
    /// Contador de doadores cadastrados.
    /// </summary>
    private readonly Counter _cadastrados;

    /// <summary>
    /// Contador de titulares anonimizados.
    /// </summary>
    private readonly Counter _anonimizados;

    /// <summary>
    /// Cria o contador no registro padrão do Prometheus.
    /// </summary>
    public MetricasDoadoresPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria o contador na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasDoadoresPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _cadastrados = fabrica.CreateCounter("doadores_cadastrados_total", "Doadores cadastrados com sucesso.");
        _anonimizados = fabrica.CreateCounter("titulares_anonimizados_total", "Titulares que tiveram o cadastro anonimizado.");
    }

    /// <inheritdoc/>
    public void RegistrarCadastro()
    {
        _cadastrados.Inc();
    }

    /// <inheritdoc/>
    public void RegistrarAnonimizacao()
    {
        _anonimizados.Inc();
    }
}
