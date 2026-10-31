using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Doacoes;
using Prometheus;

namespace Campanhas.Infrastructure.Arrecadacao;

/// <summary>
/// Métricas de doação no Prometheus: <c>doacoes_recebidas_total</c> e <c>doacoes_recusadas_total{motivo}</c> (API);
/// <c>doacoes_processadas_total{resultado}</c>, <c>doacoes_valor_reais_total</c> e <c>doacao_atraso_processamento_segundos</c> (Worker).
/// Os rótulos têm poucos valores fixos e nenhum identificador.
/// </summary>
public sealed class MetricasDoacoesPrometheus : IMetricasDoacoes, IMetricasArrecadacao
{
    /// <summary>
    /// Contador de doações aceitas pela API.
    /// </summary>
    private readonly Counter _recebidas;

    /// <summary>
    /// Contador de doações recusadas pela API, por motivo.
    /// </summary>
    private readonly Counter _recusadas;

    /// <summary>
    /// Contador de doações processadas pelo Worker, por resultado.
    /// </summary>
    private readonly Counter _processadas;

    /// <summary>
    /// Contador do valor somado aos totais, em reais.
    /// </summary>
    private readonly Counter _valor;

    /// <summary>
    /// Histograma do atraso entre o aceite e a soma, em segundos.
    /// </summary>
    private readonly Histogram _atraso;

    /// <summary>
    /// Cria as métricas no registro padrão do Prometheus.
    /// </summary>
    public MetricasDoacoesPrometheus()
        : this(Metrics.DefaultFactory)
    {
    }

    /// <summary>
    /// Cria as métricas na fábrica informada.
    /// </summary>
    /// <param name="fabrica">Fábrica de métricas (os testes passam uma com registro próprio).</param>
    public MetricasDoacoesPrometheus(IMetricFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        _recebidas = fabrica.CreateCounter("doacoes_recebidas_total", "Doações aceitas pela API.");
        _recusadas = fabrica.CreateCounter(
            "doacoes_recusadas_total",
            "Doações recusadas pela API, por motivo.",
            new CounterConfiguration { LabelNames = ["motivo"] });
        _processadas = fabrica.CreateCounter(
            "doacoes_processadas_total",
            "Doações processadas pelo Worker, por resultado.",
            new CounterConfiguration { LabelNames = ["resultado"] });
        _valor = fabrica.CreateCounter("doacoes_valor_reais_total", "Valor somado aos totais das campanhas, em reais.");
        _atraso = fabrica.CreateHistogram(
            "doacao_atraso_processamento_segundos",
            "Tempo entre o aceite da doação e a soma ao total, em segundos.",
            new HistogramConfiguration { Buckets = [0.1, 0.5, 1, 2, 5, 10, 30, 60, 120, 300] });
    }

    /// <inheritdoc/>
    public void RegistrarRecebida()
    {
        _recebidas.Inc();
    }

    /// <inheritdoc/>
    public void RegistrarRecusada(MotivoRecusa motivo)
    {
        _recusadas.WithLabels(Rotulo(motivo)).Inc();
    }

    /// <inheritdoc/>
    public void RegistrarProcessada(ResultadoProcessamento resultado)
    {
        _processadas.WithLabels(resultado.ToString().ToLowerInvariant()).Inc();
    }

    /// <inheritdoc/>
    public void RegistrarSoma(decimal valor, TimeSpan atraso)
    {
        _valor.Inc((double)valor);
        _atraso.Observe(Math.Max(0, atraso.TotalSeconds));
    }

    /// <summary>
    /// Converte o motivo no valor do rótulo, em minúsculas com sublinhado.
    /// </summary>
    /// <param name="motivo">Motivo da recusa.</param>
    /// <returns>O valor do rótulo <c>motivo</c>.</returns>
    private static string Rotulo(MotivoRecusa motivo)
    {
        return motivo switch
        {
            MotivoRecusa.Validacao => "validacao",
            MotivoRecusa.CampanhaNaoEncontrada => "campanha_nao_encontrada",
            MotivoRecusa.CampanhaNaoAtiva => "campanha_nao_ativa",
            _ => "chave_conflitante",
        };
    }
}
