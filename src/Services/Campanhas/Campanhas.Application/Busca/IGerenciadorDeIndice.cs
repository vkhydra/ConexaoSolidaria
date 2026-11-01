namespace Campanhas.Application.Busca;

/// <summary>
/// Porta de administração do índice de busca para a reindexação sem indisponibilidade: cria um índice novo ao lado do atual,
/// carrega os documentos e só então troca o alias que a busca usa.
/// </summary>
public interface IGerenciadorDeIndice
{
    /// <summary>
    /// Cria um índice físico novo, vazio e sem alias, com o mapeamento atual.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O nome do índice criado.</returns>
    /// <exception cref="BuscaIndisponivelException">O mecanismo de busca não respondeu.</exception>
    Task<string> CriarIndiceNovoAsync(CancellationToken cancelamento);

    /// <summary>
    /// Grava um lote de documentos no índice, com <see cref="CampanhaIndexada.Versao"/> como versão externa.
    /// </summary>
    /// <param name="indice">Nome do índice físico.</param>
    /// <param name="lote">Documentos a gravar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o lote gravado.</returns>
    /// <exception cref="BuscaIndisponivelException">O mecanismo de busca não respondeu.</exception>
    Task GravarLoteAsync(string indice, IReadOnlyList<CampanhaIndexada> lote, CancellationToken cancelamento);

    /// <summary>
    /// Aponta o alias da busca para o índice novo, numa única operação atômica, e apaga os índices antigos.
    /// </summary>
    /// <param name="indice">Nome do índice físico que passa a servir a busca.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o alias trocado.</returns>
    /// <exception cref="BuscaIndisponivelException">O mecanismo de busca não respondeu.</exception>
    Task PublicarIndiceAsync(string indice, CancellationToken cancelamento);

    /// <summary>
    /// Apaga um índice criado por <see cref="CriarIndiceNovoAsync"/> que não chegou a ser publicado. Não falha se ele não existe.
    /// </summary>
    /// <param name="indice">Nome do índice físico.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o índice apagado.</returns>
    Task DescartarIndiceAsync(string indice, CancellationToken cancelamento);
}
