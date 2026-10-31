using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Doacoes;
using Campanhas.Infrastructure.Arrecadacao;
using Prometheus;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes das métricas de doação no Prometheus: nomes, rótulos e valores.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MetricasDoacoesPrometheusTests
{
    /// <summary>
    /// Registro próprio do teste, para não misturar com o registro padrão do processo.
    /// </summary>
    private readonly CollectorRegistry _registro = Metrics.NewCustomRegistry();

    /// <summary>
    /// Dadas doações aceitas e recusadas, quando registradas, então os contadores somam por motivo.
    /// </summary>
    [Fact]
    public void RegistrarRecebidaERecusada_SomaPorMotivo()
    {
        var fabrica = Metrics.WithCustomRegistry(_registro);
        var metricas = new MetricasDoacoesPrometheus(fabrica);

        metricas.RegistrarRecebida();
        metricas.RegistrarRecebida();
        metricas.RegistrarRecusada(MotivoRecusa.Validacao);
        metricas.RegistrarRecusada(MotivoRecusa.CampanhaNaoEncontrada);
        metricas.RegistrarRecusada(MotivoRecusa.CampanhaNaoAtiva);
        metricas.RegistrarRecusada(MotivoRecusa.CampanhaNaoAtiva);
        metricas.RegistrarRecusada(MotivoRecusa.ChaveConflitante);

        fabrica.CreateCounter("doacoes_recebidas_total", "Doações aceitas pela API.").Value.ShouldBe(2);
        var recusadas = fabrica.CreateCounter("doacoes_recusadas_total", "Doações recusadas pela API, por motivo.", new CounterConfiguration { LabelNames = ["motivo"] });
        recusadas.WithLabels("validacao").Value.ShouldBe(1);
        recusadas.WithLabels("campanha_nao_encontrada").Value.ShouldBe(1);
        recusadas.WithLabels("campanha_nao_ativa").Value.ShouldBe(2);
        recusadas.WithLabels("chave_conflitante").Value.ShouldBe(1);
    }

    /// <summary>
    /// Dadas doações processadas, quando registradas, então o contador por resultado, o valor e o histograma de atraso refletem os registros.
    /// </summary>
    [Fact]
    public void RegistrarProcessadaESoma_AtualizaContadoresEHistograma()
    {
        var fabrica = Metrics.WithCustomRegistry(_registro);
        var metricas = new MetricasDoacoesPrometheus(fabrica);

        metricas.RegistrarProcessada(ResultadoProcessamento.Sucesso);
        metricas.RegistrarSoma(150.50m, TimeSpan.FromSeconds(1.5));
        metricas.RegistrarProcessada(ResultadoProcessamento.Duplicada);
        metricas.RegistrarProcessada(ResultadoProcessamento.Falha);
        metricas.RegistrarSoma(10m, TimeSpan.FromSeconds(-2));

        var processadas = fabrica.CreateCounter("doacoes_processadas_total", "Doações processadas pelo Worker, por resultado.", new CounterConfiguration { LabelNames = ["resultado"] });
        processadas.WithLabels("sucesso").Value.ShouldBe(1);
        processadas.WithLabels("duplicada").Value.ShouldBe(1);
        processadas.WithLabels("falha").Value.ShouldBe(1);
        fabrica.CreateCounter("doacoes_valor_reais_total", "Valor somado aos totais das campanhas, em reais.").Value.ShouldBe(160.5);
        var atraso = fabrica.CreateHistogram("doacao_atraso_processamento_segundos", "Tempo entre o aceite da doação e a soma ao total, em segundos.", new HistogramConfiguration { Buckets = [0.1, 0.5, 1, 2, 5, 10, 30, 60, 120, 300] });
        atraso.Count.ShouldBe(2);
        atraso.Sum.ShouldBe(1.5);
    }

    /// <summary>
    /// Dada uma fábrica nula, quando as métricas são criadas, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_FabricaNula_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new MetricasDoacoesPrometheus(null!));
    }
}
