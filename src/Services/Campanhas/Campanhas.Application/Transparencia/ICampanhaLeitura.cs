namespace Campanhas.Application.Transparencia;

/// <summary>
/// Linha lida do banco para o painel, antes do cálculo do percentual.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título.</param>
/// <param name="MetaFinanceira">Meta em reais.</param>
/// <param name="TotalArrecadado">Total arrecadado em reais.</param>
/// <param name="DataFim">Último dia da campanha.</param>
public sealed record CampanhaDoPainel(Guid Id, string Titulo, decimal MetaFinanceira, decimal TotalArrecadado, DateOnly DataFim);

/// <summary>
/// Porta de leitura do painel (lado de consulta): consulta direta e enxuta, sem carregar o agregado.
/// </summary>
public interface ICampanhaLeitura
{
    /// <summary>
    /// Lista as campanhas ativas cujo período inclui o dia informado, ordenadas pela data fim, até 100.
    /// </summary>
    /// <param name="hoje">Dia de referência, em São Paulo.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As campanhas do painel, mais próximas do fim primeiro.</returns>
    Task<IReadOnlyList<CampanhaDoPainel>> ListarAtivasAsync(DateOnly hoje, CancellationToken cancelamento);
}
