namespace Campanhas.Application.Doacoes;

/// <summary>
/// Motivo pelo qual uma doação foi recusada; é o rótulo <c>motivo</c> da métrica, com poucos valores para manter a cardinalidade baixa.
/// </summary>
public enum MotivoRecusa
{
    /// <summary>
    /// O pedido ou o cabeçalho <c>Idempotency-Key</c> está fora do formato.
    /// </summary>
    Validacao,

    /// <summary>
    /// A campanha não existe.
    /// </summary>
    CampanhaNaoEncontrada,

    /// <summary>
    /// A campanha não está ativa ou o dia está fora do período.
    /// </summary>
    CampanhaNaoAtiva,

    /// <summary>
    /// A <c>Idempotency-Key</c> já foi usada com outro corpo.
    /// </summary>
    ChaveConflitante,
}

/// <summary>
/// Porta das métricas de recebimento de doação na Campanhas.Api: <c>doacoes_recebidas_total</c> e
/// <c>doacoes_recusadas_total{motivo}</c>.
/// </summary>
public interface IMetricasDoacoes
{
    /// <summary>
    /// Conta uma doação aceita (202). Repetição com a mesma <c>Idempotency-Key</c> não é contada de novo.
    /// </summary>
    void RegistrarRecebida();

    /// <summary>
    /// Conta uma doação recusada.
    /// </summary>
    /// <param name="motivo">Motivo da recusa.</param>
    void RegistrarRecusada(MotivoRecusa motivo);
}
