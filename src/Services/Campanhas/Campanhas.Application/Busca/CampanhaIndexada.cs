using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.Application.Busca;

/// <summary>
/// Documento de uma campanha no índice de busca. Não carrega dado pessoal.
/// </summary>
/// <param name="Id">Identificador da campanha, também o identificador do documento.</param>
/// <param name="Titulo">Título atual.</param>
/// <param name="Descricao">Descrição atual.</param>
/// <param name="Status">Situação atual (<c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>).</param>
/// <param name="MetaFinanceira">Meta financeira, com duas casas decimais.</param>
/// <param name="DataFim">Último dia da campanha.</param>
/// <param name="Versao">Versão externa do documento: o índice ignora gravações com versão menor ou igual à já gravada.</param>
public sealed record CampanhaIndexada(
    Guid Id,
    string Titulo,
    string Descricao,
    string Status,
    decimal MetaFinanceira,
    DateOnly DataFim,
    long Versao)
{
    /// <summary>
    /// Monta o documento a partir do evento de alteração da campanha.
    /// </summary>
    /// <param name="evento">Evento recebido da fila <c>indexar-campanha</c>.</param>
    /// <returns>O documento a gravar no índice.</returns>
    public static CampanhaIndexada De(CampanhaAlteradaEvent evento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        return new CampanhaIndexada(
            evento.CampanhaId,
            evento.Titulo,
            evento.Descricao,
            evento.Status,
            evento.MetaFinanceira,
            evento.DataFim,
            evento.Versao);
    }
}
