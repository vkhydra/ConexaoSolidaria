namespace Campanhas.Application.Busca;

/// <summary>
/// Resultado da gravação de um documento no índice de busca.
/// </summary>
public enum ResultadoIndexacao
{
    /// <summary>
    /// O documento foi gravado (novo ou mais recente que o anterior).
    /// </summary>
    Indexada,

    /// <summary>
    /// Já existe uma versão igual ou mais nova; a gravação foi ignorada.
    /// </summary>
    VersaoAntiga,
}

/// <summary>
/// Porta de escrita no índice de busca. A implementação (Elasticsearch) fica na infraestrutura.
/// </summary>
public interface IIndiceCampanhas
{
    /// <summary>
    /// Grava ou substitui o documento de uma campanha usando <see cref="CampanhaIndexada.Versao"/> como versão externa, então
    /// eventos fora de ordem nunca sobrescrevem dados mais novos.
    /// </summary>
    /// <param name="campanha">Documento da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Se o documento foi gravado ou ignorado por ser uma versão antiga.</returns>
    /// <exception cref="BuscaIndisponivelException">O mecanismo de busca não respondeu.</exception>
    Task<ResultadoIndexacao> GravarAsync(CampanhaIndexada campanha, CancellationToken cancelamento);
}
