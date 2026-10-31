namespace Campanhas.Application.Arrecadacao;

/// <summary>
/// Lançamento do livro de doações: o registro imutável de uma doação somada ao total da campanha. Só tem identificadores
/// e valores, sem dado pessoal.
/// </summary>
/// <param name="DoacaoId">Identificador da doação, chave do lançamento.</param>
/// <param name="CampanhaId">Campanha que recebeu a doação.</param>
/// <param name="DoadorId">Doador que fez a doação.</param>
/// <param name="Valor">Valor doado, com duas casas decimais.</param>
/// <param name="TotalAposLancamento">Total da campanha logo depois de somar a doação.</param>
/// <param name="RecebidaEm">Instante em que a API aceitou a doação.</param>
/// <param name="ProcessadaEm">Instante em que a doação foi somada ao total.</param>
public sealed record LancamentoDoacao(
    Guid DoacaoId,
    Guid CampanhaId,
    Guid DoadorId,
    decimal Valor,
    decimal TotalAposLancamento,
    DateTimeOffset RecebidaEm,
    DateTimeOffset ProcessadaEm);

/// <summary>
/// Porta do livro de doações (MongoDB), o histórico auditável de tudo que foi somado aos totais. A gravação é idempotente:
/// repetir o mesmo lançamento não altera o que já está no livro.
/// </summary>
public interface ILivroDoacoes
{
    /// <summary>
    /// Grava o lançamento no livro, se ainda não existir.
    /// </summary>
    /// <param name="lancamento">Lançamento da doação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o lançamento está no livro.</returns>
    Task RegistrarAsync(LancamentoDoacao lancamento, CancellationToken cancelamento);
}
