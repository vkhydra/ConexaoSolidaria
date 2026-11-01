using System.Text.Json.Serialization;
using Campanhas.Application.Busca;
using Elastic.Clients.Elasticsearch;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Grava campanhas no índice do Elasticsearch com versão externa: o documento só é aceito se a versão for maior que a já
/// gravada, então eventos entregues fora de ordem ou repetidos não desfazem dados mais novos.
/// </summary>
/// <param name="cliente">Cliente do Elasticsearch.</param>
public sealed class IndiceCampanhasElasticsearch(ElasticsearchClient cliente) : IIndiceCampanhas
{
    /// <summary>
    /// Indica que o alias já foi conferido neste processo.
    /// </summary>
    private volatile bool _aliasPronto;

    /// <inheritdoc/>
    public async Task<ResultadoIndexacao> GravarAsync(CampanhaIndexada campanha, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        await GarantirAliasAsync(cancelamento);

        var resposta = await cliente.IndexAsync(
            DocumentoCampanha.De(campanha),
            requisicao => requisicao
                .Index(MapeamentoIndice.NomeAlias)
                .Id(campanha.Id.ToString())
                .VersionType(VersionType.External)
                .Version(campanha.Versao),
            cancelamento);

        if (resposta.ApiCallDetails.HttpStatusCode == 409)
        {
            return ResultadoIndexacao.VersaoAntiga;
        }

        FalhasDoElasticsearch.ExigirSucesso(resposta.ApiCallDetails, "indexar campanha");
        return ResultadoIndexacao.Indexada;
    }

    /// <summary>
    /// Cria o índice inicial com o alias na primeira gravação, se ainda não existir nenhum.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o alias pronto.</returns>
    private async Task GarantirAliasAsync(CancellationToken cancelamento)
    {
        if (_aliasPronto)
        {
            return;
        }

        // A criação é idempotente (índice que já existe é aceito), então gravações simultâneas na partida não precisam de trava.
        await MapeamentoIndice.GarantirAliasAsync(cliente, cancelamento);
        _aliasPronto = true;
    }

    /// <summary>
    /// Forma do documento no índice, com os nomes de campo fixados para não depender de convenção do serializador.
    /// </summary>
    /// <param name="Titulo">Título da campanha.</param>
    /// <param name="Descricao">Descrição da campanha.</param>
    /// <param name="Status">Situação da campanha.</param>
    /// <param name="MetaFinanceira">Meta financeira.</param>
    /// <param name="DataFim">Último dia da campanha.</param>
    /// <param name="Versao">Versão do documento.</param>
    public sealed record DocumentoCampanha(
        [property: JsonPropertyName("titulo")] string Titulo,
        [property: JsonPropertyName("descricao")] string Descricao,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("metaFinanceira")] decimal MetaFinanceira,
        [property: JsonPropertyName("dataFim")] DateOnly DataFim,
        [property: JsonPropertyName("versao")] long Versao)
    {
        /// <summary>
        /// Converte o documento da aplicação na forma gravada no índice.
        /// </summary>
        /// <param name="campanha">Documento da aplicação.</param>
        /// <returns>O documento do índice.</returns>
        public static DocumentoCampanha De(CampanhaIndexada campanha)
        {
            return new DocumentoCampanha(campanha.Titulo, campanha.Descricao, campanha.Status, campanha.MetaFinanceira, campanha.DataFim, campanha.Versao);
        }
    }
}
