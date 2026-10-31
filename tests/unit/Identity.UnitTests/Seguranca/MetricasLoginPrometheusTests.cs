using Identity.Application.Abstracoes;
using Identity.Infrastructure;
using Prometheus;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes da métrica <c>logins_total{resultado}</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MetricasLoginPrometheusTests
{
    /// <summary>
    /// Dados logins de resultados diferentes, quando registrados, então cada rótulo conta separado.
    /// </summary>
    [Fact]
    public void Registrar_ResultadosDiferentes_ContaPorRotulo()
    {
        var registro = Metrics.NewCustomRegistry();
        var fabrica = Metrics.WithCustomRegistry(registro);
        var metricas = new MetricasLoginPrometheus(fabrica);

        metricas.Registrar(ResultadoLogin.Sucesso);
        metricas.Registrar(ResultadoLogin.Falha);
        metricas.Registrar(ResultadoLogin.Falha);
        metricas.Registrar(ResultadoLogin.Bloqueado);

        var contador = fabrica.CreateCounter("logins_total", "Tentativas de login por resultado.", new CounterConfiguration { LabelNames = ["resultado"] });
        contador.WithLabels("sucesso").Value.ShouldBe(1);
        contador.WithLabels("falha").Value.ShouldBe(2);
        contador.WithLabels("bloqueado").Value.ShouldBe(1);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando a métrica é criada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_FabricaNula_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new MetricasLoginPrometheus(null!));
    }
}
