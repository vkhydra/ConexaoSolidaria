namespace ConexaoSolidaria.Contracts.V1;

/// <summary>
/// Publicado pelo Arrecadacao.Worker depois de somar a doação ao total da campanha. Carrega só identificadores,
/// sem dado pessoal. A versão do contrato é <c>V1</c>: campos só podem ser acrescentados no fim, nunca removidos
/// nem renomeados.
/// </summary>
/// <param name="DoacaoId">Identificador da doação processada.</param>
/// <param name="CampanhaId">Campanha que recebeu a doação.</param>
/// <param name="DoadorId">Doador que fez a doação.</param>
/// <param name="Valor">Valor doado, com duas casas decimais.</param>
/// <param name="TotalArrecadado">Total da campanha logo depois de somar esta doação.</param>
/// <param name="RecebidaEm">Instante em que a doação foi aceita pela API.</param>
/// <param name="ProcessadaEm">Instante em que o Worker somou a doação ao total.</param>
public sealed record DoacaoProcessadaEvent(
    Guid DoacaoId,
    Guid CampanhaId,
    Guid DoadorId,
    decimal Valor,
    decimal TotalArrecadado,
    DateTimeOffset RecebidaEm,
    DateTimeOffset ProcessadaEm);
