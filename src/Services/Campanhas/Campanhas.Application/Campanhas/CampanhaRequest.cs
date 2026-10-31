namespace Campanhas.Application.Campanhas;

/// <summary>
/// Dados de uma campanha enviados pelo gestor, na criação e na edição.
/// </summary>
/// <param name="Titulo">Título, de 3 a 120 caracteres e sem espaços nas pontas.</param>
/// <param name="Descricao">Descrição, de até 2000 caracteres.</param>
/// <param name="DataInicio">Primeiro dia da campanha.</param>
/// <param name="DataFim">Último dia da campanha.</param>
/// <param name="MetaFinanceira">Meta em reais, com até duas casas decimais.</param>
public sealed record CampanhaRequest(
    string? Titulo,
    string? Descricao,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    decimal? MetaFinanceira);
