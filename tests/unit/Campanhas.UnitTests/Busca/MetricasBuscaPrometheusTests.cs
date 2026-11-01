using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Prometheus;

namespace Campanhas.UnitTests.Busca;

/// <summary>
/// Testes das métricas da busca no Prometheus: nomes, rótulos e valores.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MetricasBuscaPrometheusTests
{
    /// <summary>
    /// Registro próprio do teste, para não misturar com o registro padrão do processo.
    /// </summary>
    private readonly CollectorRegistry _registro = Metrics.NewCustomRegistry();

    /// <summary>
    /// Dadas buscas com cada resultado e campanhas indexadas, quando registradas, então os contadores somam por rótulo.
    /// </summary>
    [Fact]
    public void Registrar_BuscasEIndexadas_SomamPorRotulo()
    {
        var fabrica = Metrics.WithCustomRegistry(_registro);
        var metricas = new MetricasBuscaPrometheus(fabrica);

        metricas.RegistrarBusca(ResultadoDaBusca.Ok);
        metricas.RegistrarBusca(ResultadoDaBusca.Ok);
        metricas.RegistrarBusca(ResultadoDaBusca.Vazio);
        metricas.RegistrarBusca(ResultadoDaBusca.Indisponivel);
        metricas.RegistrarIndexada();
        metricas.RegistrarIndexada();
        metricas.RegistrarIndexada();

        var buscas = fabrica.CreateCounter("buscas_total", "Buscas de campanhas, por resultado.", new CounterConfiguration { LabelNames = ["resultado"] });
        buscas.WithLabels("ok").Value.ShouldBe(2);
        buscas.WithLabels("vazio").Value.ShouldBe(1);
        buscas.WithLabels("indisponivel").Value.ShouldBe(1);
        fabrica.CreateCounter("campanhas_indexadas_total", "Campanhas gravadas no índice de busca.").Value.ShouldBe(3);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando as métricas são criadas, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_FabricaNula_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new MetricasBuscaPrometheus(null!));
    }

    /// <summary>
    /// Dado o construtor sem argumentos, quando usado, então registra no registro padrão sem falhar.
    /// </summary>
    [Fact]
    public void Construtor_Padrao_UsaORegistroPadrao()
    {
        Should.NotThrow(() => _ = new MetricasBuscaPrometheus());
    }
}
