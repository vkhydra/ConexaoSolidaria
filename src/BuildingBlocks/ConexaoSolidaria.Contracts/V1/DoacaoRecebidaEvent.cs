namespace ConexaoSolidaria.Contracts.V1;

/// <summary>
/// Publicado pela Campanhas.Api quando uma doação é aceita; o valor ainda não foi somado ao total da campanha.
/// Carrega só identificadores, sem dado pessoal. A versão do contrato é <c>V1</c>: campos só podem ser acrescentados
/// no fim, nunca removidos nem renomeados.
/// </summary>
/// <param name="DoacaoId">Identificador da doação, chave de idempotência do processamento.</param>
/// <param name="CampanhaId">Campanha que recebe a doação.</param>
/// <param name="DoadorId">Doador autenticado que fez a doação.</param>
/// <param name="Valor">Valor doado, com duas casas decimais.</param>
/// <param name="OcorridoEm">Instante em que a doação foi aceita.</param>
public sealed record DoacaoRecebidaEvent(
    Guid DoacaoId,
    Guid CampanhaId,
    Guid DoadorId,
    decimal Valor,
    DateTimeOffset OcorridoEm);
