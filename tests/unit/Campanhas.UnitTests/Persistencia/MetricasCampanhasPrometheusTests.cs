using Campanhas.Infrastructure;
using Prometheus;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes da métrica <c>campanhas_criadas_total</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MetricasCampanhasPrometheusTests
{
    /// <summary>
    /// Dadas duas criações, quando registradas, então o contador soma dois.
    /// </summary>
    [Fact]
    public void RegistrarCriacao_DuasCriacoes_SomaDois()
    {
        var fabrica = Metrics.WithCustomRegistry(Metrics.NewCustomRegistry());
        var metricas = new MetricasCampanhasPrometheus(fabrica);

        metricas.RegistrarCriacao();
        metricas.RegistrarCriacao();

        fabrica.CreateCounter("campanhas_criadas_total", "Campanhas criadas com sucesso.").Value.ShouldBe(2);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando a métrica é criada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_FabricaNula_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new MetricasCampanhasPrometheus(null!));
    }
}
