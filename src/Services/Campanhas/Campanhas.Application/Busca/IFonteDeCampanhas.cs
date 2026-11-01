namespace Campanhas.Application.Busca;

/// <summary>
/// Porta de leitura de todas as campanhas do banco, em lotes, para reconstruir o índice de busca. A fonte da verdade é o
/// PostgreSQL; o índice é derivado dele.
/// </summary>
public interface IFonteDeCampanhas
{
    /// <summary>
    /// Lê todas as campanhas (de qualquer status) em lotes ordenados pelo identificador.
    /// </summary>
    /// <param name="tamanhoDoLote">Quantidade de campanhas por lote.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os lotes, na ordem; o último pode ser menor que <paramref name="tamanhoDoLote"/>.</returns>
    IAsyncEnumerable<IReadOnlyList<CampanhaIndexada>> LerEmLotesAsync(int tamanhoDoLote, CancellationToken cancelamento);
}
