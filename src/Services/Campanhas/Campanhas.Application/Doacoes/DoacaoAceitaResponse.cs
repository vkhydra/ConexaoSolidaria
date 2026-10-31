namespace Campanhas.Application.Doacoes;

/// <summary>
/// Resposta do aceite de uma doação (202): ela foi recebida, mas o total da campanha só muda quando o Worker a processar.
/// </summary>
/// <param name="DoacaoId">Identificador da doação, para acompanhamento.</param>
/// <param name="Status">Situação da doação; sempre <c>Recebida</c> neste momento.</param>
/// <param name="Mensagem">Texto para o doador.</param>
public sealed record DoacaoAceitaResponse(Guid DoacaoId, string Status, string Mensagem);
