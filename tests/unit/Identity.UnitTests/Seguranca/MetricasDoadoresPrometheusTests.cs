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
    /// Dadas três anonimizações, quando registradas, então o contador de titulares soma três e não mexe no de cadastros.
    /// </summary>
    [Fact]
    public void RegistrarAnonimizacao_TresAnonimizacoes_SomaTres()
    {
        var fabrica = Metrics.WithCustomRegistry(Metrics.NewCustomRegistry());
        var metricas = new MetricasDoadoresPrometheus(fabrica);

        metricas.RegistrarAnonimizacao();
        metricas.RegistrarAnonimizacao();
        metricas.RegistrarAnonimizacao();

        fabrica.CreateCounter("titulares_anonimizados_total", "Titulares que tiveram o cadastro anonimizado.").Value.ShouldBe(3);
        fabrica.CreateCounter("doadores_cadastrados_total", "Doadores cadastrados com sucesso.").Value.ShouldBe(0);
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
