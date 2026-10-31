namespace Campanhas.Application.Comum;

/// <summary>
/// Uma página de resultados e o total de itens que passam no filtro.
/// </summary>
/// <typeparam name="T">Tipo dos itens.</typeparam>
/// <param name="Itens">Itens da página, na ordem pedida.</param>
/// <param name="Numero">Número da página, a partir de 1.</param>
/// <param name="TamanhoPagina">Itens por página.</param>
/// <param name="Total">Total de itens em todas as páginas.</param>
public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Numero, int TamanhoPagina, int Total);
