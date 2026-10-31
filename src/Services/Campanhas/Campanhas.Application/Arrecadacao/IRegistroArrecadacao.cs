using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.Application.Arrecadacao;

/// <summary>
/// Situação de uma doação depois da tentativa de registro no total da campanha.
/// </summary>
public enum SituacaoRegistro
{
    /// <summary>
    /// A doação foi somada ao total agora e o <c>DoacaoProcessadaEvent</c> entrou no Outbox.
    /// </summary>
    Registrada,

    /// <summary>
    /// A doação já tinha sido somada antes (reentrega da mensagem); nada foi alterado.
    /// </summary>
    JaProcessada,

    /// <summary>
    /// A campanha da doação não existe; a mensagem não pode ser processada.
    /// </summary>
    CampanhaInexistente,
}

/// <summary>
/// Resultado do registro de uma doação.
/// </summary>
/// <param name="Situacao">O que aconteceu com a doação.</param>
/// <param name="ProcessadaEm">Instante em que a doação foi somada ao total (o da primeira vez, em caso de reentrega); nulo se a campanha não existe.</param>
/// <param name="TotalArrecadado">
/// Total da campanha logo depois de somar a doação; em caso de reentrega, o total atual da campanha. Nulo se a campanha não existe.
/// </param>
public sealed record ResultadoRegistro(SituacaoRegistro Situacao, DateTimeOffset? ProcessadaEm, decimal? TotalArrecadado);

/// <summary>
/// Porta que soma uma doação ao total da campanha de forma atômica e idempotente. A implementação (PostgreSQL) faz, em uma
/// só transação, o registro da doação processada, o <c>UPDATE</c> do total e a gravação do <c>DoacaoProcessadaEvent</c> no Outbox.
/// </summary>
public interface IRegistroArrecadacao
{
    /// <summary>
    /// Soma a doação ao total da campanha, uma única vez por <see cref="DoacaoRecebidaEvent.DoacaoId"/>.
    /// </summary>
    /// <param name="doacao">Doação recebida.</param>
    /// <param name="agora">Instante do processamento.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A situação da doação, com o total da campanha.</returns>
    Task<ResultadoRegistro> RegistrarAsync(DoacaoRecebidaEvent doacao, DateTimeOffset agora, CancellationToken cancelamento);
}
