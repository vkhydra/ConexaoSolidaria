namespace Campanhas.Application.Transparencia;

/// <summary>
/// Campanha ativa mostrada no painel público de transparência. Não carrega dado pessoal.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título.</param>
/// <param name="MetaFinanceira">Meta em reais.</param>
/// <param name="TotalArrecadado">Total arrecadado em reais.</param>
/// <param name="PercentualAtingido">Percentual da meta já arrecadado, com duas casas.</param>
/// <param name="DataFim">Último dia da campanha.</param>
public sealed record PainelItemResponse(
    Guid Id,
    string Titulo,
    decimal MetaFinanceira,
    decimal TotalArrecadado,
    decimal PercentualAtingido,
    DateOnly DataFim);
