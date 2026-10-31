using Campanhas.Domain.Campanhas;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Campanha devolvida pela API de gestão.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="DataInicio">Primeiro dia.</param>
/// <param name="DataFim">Último dia.</param>
/// <param name="MetaFinanceira">Meta em reais.</param>
/// <param name="TotalArrecadado">Total arrecadado em reais.</param>
/// <param name="PercentualAtingido">Percentual da meta já arrecadado, com duas casas.</param>
/// <param name="Status">Situação: <c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>.</param>
/// <param name="Versao">Versão da linha, devolvida no cabeçalho <c>ETag</c> e não no corpo.</param>
public sealed record CampanhaResponse(
    Guid Id,
    string Titulo,
    string Descricao,
    DateOnly DataInicio,
    DateOnly DataFim,
    decimal MetaFinanceira,
    decimal TotalArrecadado,
    decimal PercentualAtingido,
    string Status,
    [property: System.Text.Json.Serialization.JsonIgnore] uint Versao)
{
    /// <summary>
    /// Monta a resposta a partir do agregado.
    /// </summary>
    /// <param name="campanha">Campanha carregada.</param>
    /// <returns>A resposta, com o percentual atingido calculado.</returns>
    public static CampanhaResponse De(Campanha campanha)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        var percentual = campanha.MetaFinanceira.Valor == 0m
            ? 0m
            : decimal.Round(campanha.TotalArrecadado.Valor / campanha.MetaFinanceira.Valor * 100m, 2, MidpointRounding.ToEven);

        return new CampanhaResponse(
            campanha.Id.Valor,
            campanha.Titulo,
            campanha.Descricao,
            campanha.Periodo.DataInicio,
            campanha.Periodo.DataFim,
            campanha.MetaFinanceira.Valor,
            campanha.TotalArrecadado.Valor,
            percentual,
            campanha.Status.ToString(),
            campanha.Versao);
    }
}
