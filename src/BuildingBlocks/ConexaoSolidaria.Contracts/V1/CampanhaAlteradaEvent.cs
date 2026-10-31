namespace ConexaoSolidaria.Contracts.V1;

/// <summary>
/// Publicado sempre que uma campanha é criada ou alterada. Alimenta o índice de busca. Não carrega dado pessoal.
/// A versão do contrato é <c>V1</c>: campos só podem ser acrescentados no fim, nunca removidos nem renomeados.
/// </summary>
/// <param name="CampanhaId">Identificador da campanha.</param>
/// <param name="Titulo">Título atual.</param>
/// <param name="Descricao">Descrição atual.</param>
/// <param name="Status">Situação atual (<c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>).</param>
/// <param name="MetaFinanceira">Meta financeira, com duas casas decimais.</param>
/// <param name="DataFim">Último dia da campanha.</param>
/// <param name="Versao">Marcador crescente da alteração (ticks UTC do instante da alteração); o consumidor ignora versões antigas.</param>
public sealed record CampanhaAlteradaEvent(
    Guid CampanhaId,
    string Titulo,
    string Descricao,
    string Status,
    decimal MetaFinanceira,
    DateOnly DataFim,
    long Versao);
