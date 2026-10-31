using Identity.Infrastructure;
using Prometheus;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes da métrica <c>doadores_cadastrados_total</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MetricasDoadoresPrometheusTests
{
    /// <summary>
    /// Dados dois cadastros, quando registrados, então o contador soma dois.
    /// </summary>
    [Fact]
    public void RegistrarCadastro_DoisCadastros_SomaDois()
    {
        var fabrica = Metrics.WithCustomRegistry(Metrics.NewCustomRegistry());
        var metricas = new MetricasDoadoresPrometheus(fabrica);

        metricas.RegistrarCadastro();
        metricas.RegistrarCadastro();

        fabrica.CreateCounter("doadores_cadastrados_total", "Doadores cadastrados com sucesso.").Value.ShouldBe(2);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando a métrica é criada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_FabricaNula_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new MetricasDoadoresPrometheus(null!));
    }
}
