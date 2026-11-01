using Campanhas.Application.Busca;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Definição do índice de campanhas: analisador em português (<c>pt_br</c>), mapeamento dos campos e alias <c>campanhas</c>.
/// O índice físico é versionado (<c>campanhas-v1</c>, <c>campanhas-v2</c>…) e o alias é trocado na reindexação.
/// </summary>
public static class MapeamentoIndice
{
    /// <summary>
    /// Nome do alias que a busca e a indexação usam.
    /// </summary>
    public const string NomeAlias = "campanhas";

    /// <summary>
    /// Nome do primeiro índice físico, criado quando ainda não existe nenhum.
    /// </summary>
    public const string NomeIndiceInicial = "campanhas-v1";

    /// <summary>
    /// Configurações e mapeamento do índice, sem alias.
    /// </summary>
    private const string _definicao = """
        "settings": {
          "number_of_shards": 1,
          "number_of_replicas": 0,
          "analysis": {
            "normalizer": {
              "lowercase": { "type": "custom", "filter": ["lowercase"] }
            },
            "analyzer": {
              "pt_br": { "tokenizer": "standard", "filter": ["lowercase", "asciifolding", "stop_pt", "stemmer_pt"] }
            },
            "filter": {
              "stop_pt": { "type": "stop", "stopwords": "_brazilian_" },
              "stemmer_pt": { "type": "stemmer", "language": "brazilian" }
            }
          }
        },
        "mappings": {
          "properties": {
            "titulo": { "type": "text", "analyzer": "pt_br", "fields": { "exato": { "type": "keyword", "normalizer": "lowercase" } } },
            "descricao": { "type": "text", "analyzer": "pt_br" },
            "status": { "type": "keyword" },
            "metaFinanceira": { "type": "scaled_float", "scaling_factor": 100 },
            "dataFim": { "type": "date", "format": "yyyy-MM-dd" },
            "versao": { "type": "long" }
          }
        }
        """;

    /// <summary>
    /// Monta o corpo da criação de um índice.
    /// </summary>
    /// <param name="comAlias">Se o índice já nasce com o alias <see cref="NomeAlias"/>; na reindexação o alias só é trocado ao final.</param>
    /// <returns>O JSON da requisição <c>PUT /{índice}</c>.</returns>
    public static string CorpoDaCriacao(bool comAlias)
    {
        var aliases = comAlias ? $"\"aliases\": {{ \"{NomeAlias}\": {{}} }}," : string.Empty;
        return $"{{ {aliases} {_definicao} }}";
    }

    /// <summary>
    /// Cria um índice físico com o mapeamento de campanhas. Não faz nada se ele já existir.
    /// </summary>
    /// <param name="cliente">Cliente do Elasticsearch.</param>
    /// <param name="nomeIndice">Nome do índice físico (por exemplo, <c>campanhas-v2</c>).</param>
    /// <param name="comAlias">Se o índice já nasce com o alias <see cref="NomeAlias"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o índice criado ou já existente.</returns>
    /// <exception cref="BuscaIndisponivelException">O Elasticsearch não respondeu.</exception>
    /// <exception cref="InvalidOperationException">O Elasticsearch recusou a criação por outro motivo que não a existência prévia.</exception>
    public static async Task CriarIndiceAsync(ElasticsearchClient cliente, string nomeIndice, bool comAlias, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeIndice);

        var resposta = await cliente.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.PUT,
            $"/{nomeIndice}",
            PostData.String(CorpoDaCriacao(comAlias)),
            cancellationToken: cancelamento);

        if (!resposta.ApiCallDetails.HasSuccessfulStatusCode)
        {
            if (FalhasDoElasticsearch.EhIndisponibilidade(resposta.ApiCallDetails.HttpStatusCode))
            {
                throw new BuscaIndisponivelException($"O Elasticsearch não criou o índice {nomeIndice}.", resposta.ApiCallDetails.OriginalException ?? new HttpRequestException());
            }

            if (!resposta.Body.Contains("resource_already_exists_exception", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"O Elasticsearch recusou a criação do índice {nomeIndice} (HTTP {resposta.ApiCallDetails.HttpStatusCode}).");
            }
        }
    }

    /// <summary>
    /// Garante que o alias <see cref="NomeAlias"/> existe, criando <see cref="NomeIndiceInicial"/> se nada existir ainda.
    /// </summary>
    /// <param name="cliente">Cliente do Elasticsearch.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina com o alias pronto.</returns>
    /// <exception cref="BuscaIndisponivelException">O Elasticsearch não respondeu.</exception>
    public static async Task GarantirAliasAsync(ElasticsearchClient cliente, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        var existe = await cliente.Indices.ExistsAliasAsync(NomeAlias, cancelamento);
        if (FalhasDoElasticsearch.EhIndisponibilidade(existe.ApiCallDetails.HttpStatusCode))
        {
            throw new BuscaIndisponivelException("O Elasticsearch não respondeu à consulta do alias.", existe.ApiCallDetails.OriginalException ?? new HttpRequestException());
        }

        if (!existe.Exists)
        {
            await CriarIndiceAsync(cliente, NomeIndiceInicial, comAlias: true, cancelamento);
        }
    }
}
