using Campanhas.Application.Comum;

namespace Campanhas.Application.Busca;

/// <summary>
/// Porta de consulta ao índice de busca. A implementação (Elasticsearch) fica na infraestrutura.
/// </summary>
public interface IBuscaCampanhas
{
    /// <summary>
    /// Procura campanhas ativas pelo texto, tolerando erros de digitação, com título pesando mais que a descrição.
    /// </summary>
    /// <param name="texto">Texto digitado, já sem espaços nas pontas; é sempre tratado como texto, nunca como sintaxe de consulta.</param>
    /// <param name="pagina">Número da página, a partir de 1.</param>
    /// <param name="tamanhoPagina">Itens por página, de 1 a 50.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página pedida e o total de campanhas que casam.</returns>
    /// <exception cref="BuscaIndisponivelException">O mecanismo de busca não respondeu.</exception>
    Task<Pagina<BuscaItemResponse>> BuscarAsync(string texto, int pagina, int tamanhoPagina, CancellationToken cancelamento);
}
