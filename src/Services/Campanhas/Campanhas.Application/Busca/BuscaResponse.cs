namespace Campanhas.Application.Busca;

/// <summary>
/// Uma campanha encontrada pela busca.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título da campanha.</param>
/// <param name="Trecho">Trecho da descrição com os termos encontrados entre <c>&lt;em&gt;</c>; nulo se a descrição não casou.</param>
/// <param name="Relevancia">Pontuação do mecanismo de busca; quanto maior, mais relevante.</param>
/// <param name="DataFim">Último dia da campanha.</param>
public sealed record BuscaItemResponse(Guid Id, string Titulo, string? Trecho, double Relevancia, DateOnly DataFim);

/// <summary>
/// Página de resultados da busca pública.
/// </summary>
/// <param name="Items">Campanhas da página, da mais para a menos relevante.</param>
/// <param name="Page">Número da página.</param>
/// <param name="PageSize">Itens por página.</param>
/// <param name="TotalItems">Total de campanhas que casam com a busca.</param>
/// <param name="TotalPages">Total de páginas.</param>
public sealed record BuscaResponse(IReadOnlyList<BuscaItemResponse> Items, int Page, int PageSize, int TotalItems, int TotalPages);
