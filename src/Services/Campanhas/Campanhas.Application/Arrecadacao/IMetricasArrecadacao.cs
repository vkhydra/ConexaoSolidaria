namespace Campanhas.Application.Arrecadacao;

/// <summary>
/// Resultado do processamento de uma doação no Worker; é o rótulo <c>resultado</c> da métrica.
/// </summary>
public enum ResultadoProcessamento
{
    /// <summary>
    /// A doação foi somada ao total agora.
    /// </summary>
    Sucesso,

    /// <summary>
    /// Era uma reentrega de doação já somada; o total não mudou.
    /// </summary>
    Duplicada,

    /// <summary>
    /// Falha permanente (valor inválido ou campanha inexistente); a mensagem vai para a fila de erro.
    /// </summary>
    Falha,
}

/// <summary>
/// Porta das métricas do Arrecadacao.Worker: <c>doacoes_processadas_total{resultado}</c>, <c>doacoes_valor_reais_total</c>
/// e <c>doacao_atraso_processamento_segundos</c>.
/// </summary>
public interface IMetricasArrecadacao
{
    /// <summary>
    /// Conta uma doação processada.
    /// </summary>
    /// <param name="resultado">Resultado do processamento.</param>
    void RegistrarProcessada(ResultadoProcessamento resultado);

    /// <summary>
    /// Registra o valor somado ao total e o atraso entre o aceite da doação e a soma; só para doações somadas agora.
    /// </summary>
    /// <param name="valor">Valor somado, em reais.</param>
    /// <param name="atraso">Tempo entre o aceite pela API e a soma pelo Worker.</param>
    void RegistrarSoma(decimal valor, TimeSpan atraso);
}
