using System.Globalization;
using System.Text.Json;
using Campanhas.Application.Busca;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Administra os índices físicos <c>campanhas-v*</c> no Elasticsearch para a reindexação sem indisponibilidade: cria um
/// índice novo, grava os lotes em massa (<c>_bulk</c>) e troca o alias <c>campanhas</c> numa só operação atômica.
/// </summary>
/// <param name="cliente">Cliente do Elasticsearch.</param>
/// <param name="relogio">Relógio, que dá nome único ao índice novo.</param>
public sealed class GerenciadorDeIndiceElasticsearch(ElasticsearchClient cliente, TimeProvider relogio) : IGerenciadorDeIndice
{
    /// <summary>
    /// Padrão dos nomes dos índices físicos.
    /// </summary>
    private const string _padraoDosIndices = "campanhas-v*";

    /// <inheritdoc/>
    public async Task<string> CriarIndiceNovoAsync(CancellationToken cancelamento)
    {
        var nome = $"campanhas-v{relogio.GetUtcNow().ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}";
        await MapeamentoIndice.CriarIndiceAsync(cliente, nome, comAlias: false, cancelamento);
        return nome;
    }

    /// <inheritdoc/>
    public async Task GravarLoteAsync(string indice, IReadOnlyList<CampanhaIndexada> lote, CancellationToken cancelamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indice);
        ArgumentNullException.ThrowIfNull(lote);

        if (lote.Count == 0)
        {
            return;
        }

        var linhas = new List<string>(lote.Count * 2);
        foreach (var campanha in lote)
        {
            linhas.Add(JsonSerializer.Serialize(new { index = new { _index = indice, _id = campanha.Id.ToString(), version = campanha.Versao, version_type = "external" } }));
            linhas.Add(JsonSerializer.Serialize(IndiceCampanhasElasticsearch.DocumentoCampanha.De(campanha)));
        }

        var resposta = await EnviarAsync(Elastic.Transport.HttpMethod.POST, "/_bulk", PostData.MultiJson(linhas), cancelamento);
        FalhasDoElasticsearch.ExigirSucesso(resposta.ApiCallDetails, "gravar lote de campanhas");

        using var corpo = JsonDocument.Parse(resposta.Body);
        if (corpo.RootElement.GetProperty("errors").GetBoolean())
        {
            throw new InvalidOperationException($"O Elasticsearch recusou documentos do lote no índice {indice}.");
        }
    }

    /// <inheritdoc/>
    public async Task PublicarIndiceAsync(string indice, CancellationToken cancelamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indice);

        FalhasDoElasticsearch.ExigirSucesso((await EnviarAsync(Elastic.Transport.HttpMethod.POST, $"/{indice}/_refresh", null, cancelamento)).ApiCallDetails, "atualizar índice novo");

        var troca = JsonSerializer.Serialize(new
        {
            actions = new object[]
            {
                new { remove = new { index = _padraoDosIndices, alias = MapeamentoIndice.NomeAlias, must_exist = false } },
                new { add = new { index = indice, alias = MapeamentoIndice.NomeAlias } },
            },
        });
        FalhasDoElasticsearch.ExigirSucesso((await EnviarAsync(Elastic.Transport.HttpMethod.POST, "/_aliases", PostData.String(troca), cancelamento)).ApiCallDetails, "trocar alias");

        var listagem = await EnviarAsync(Elastic.Transport.HttpMethod.GET, $"/_cat/indices/{_padraoDosIndices}?h=index&format=json", null, cancelamento);
        FalhasDoElasticsearch.ExigirSucesso(listagem.ApiCallDetails, "listar índices");
        using var indices = JsonDocument.Parse(listagem.Body);
        foreach (var antigo in indices.RootElement.EnumerateArray().Select(i => i.GetProperty("index").GetString()!).Where(nome => nome != indice))
        {
            await DescartarIndiceAsync(antigo, cancelamento);
        }
    }

    /// <inheritdoc/>
    public async Task DescartarIndiceAsync(string indice, CancellationToken cancelamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indice);

        var resposta = await EnviarAsync(Elastic.Transport.HttpMethod.DELETE, $"/{indice}", null, cancelamento);
        if (resposta.ApiCallDetails.HttpStatusCode != 404)
        {
            FalhasDoElasticsearch.ExigirSucesso(resposta.ApiCallDetails, "descartar índice");
        }
    }

    /// <summary>
    /// Envia uma requisição HTTP bruta ao Elasticsearch.
    /// </summary>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="caminho">Caminho e consulta da requisição.</param>
    /// <param name="corpo">Corpo da requisição, ou nulo.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A resposta, com o corpo como texto.</returns>
    private Task<StringResponse> EnviarAsync(Elastic.Transport.HttpMethod metodo, string caminho, PostData? corpo, CancellationToken cancelamento)
    {
        return cliente.Transport.RequestAsync<StringResponse>(metodo, caminho, corpo, cancellationToken: cancelamento);
    }
}
