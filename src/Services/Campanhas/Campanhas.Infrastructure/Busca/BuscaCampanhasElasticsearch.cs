using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campanhas.Application.Busca;
using Campanhas.Application.Comum;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Consulta o índice de campanhas no Elasticsearch: busca em título (peso 3) e descrição (peso 1) com tolerância a erros de
/// digitação, bônus para a frase no título, filtro só de campanhas <c>Ativa</c> e trecho destacado da descrição.
/// O texto digitado entra sempre como valor de um <c>multi_match</c>, nunca como <c>query_string</c>, então caracteres
/// especiais não viram sintaxe de consulta.
/// </summary>
/// <param name="cliente">Cliente do Elasticsearch.</param>
public sealed class BuscaCampanhasElasticsearch(ElasticsearchClient cliente) : IBuscaCampanhas
{
    /// <summary>
    /// Status das campanhas que aparecem na busca pública.
    /// </summary>
    private const string _statusVisivel = "Ativa";

    /// <inheritdoc/>
    public async Task<Pagina<BuscaItemResponse>> BuscarAsync(string texto, int pagina, int tamanhoPagina, CancellationToken cancelamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);

        var resposta = await cliente.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.POST,
            $"/{MapeamentoIndice.NomeAlias}/_search",
            PostData.String(MontarConsulta(texto, pagina, tamanhoPagina).ToJsonString()),
            cancellationToken: cancelamento);

        // Antes do primeiro evento o alias ainda não existe: não há campanha para achar.
        if (resposta.ApiCallDetails.HttpStatusCode == 404)
        {
            return new Pagina<BuscaItemResponse>([], pagina, tamanhoPagina, 0);
        }

        FalhasDoElasticsearch.ExigirSucesso(resposta.ApiCallDetails, "buscar campanhas");
        return LerResposta(resposta.Body, pagina, tamanhoPagina);
    }

    /// <summary>
    /// Monta o corpo da consulta: <c>multi_match</c> difuso, bônus de frase no título, filtro de status, destaque e ordenação.
    /// </summary>
    /// <param name="texto">Texto digitado.</param>
    /// <param name="pagina">Número da página, a partir de 1.</param>
    /// <param name="tamanhoPagina">Itens por página.</param>
    /// <returns>O corpo JSON da requisição de busca.</returns>
    internal static JsonObject MontarConsulta(string texto, int pagina, int tamanhoPagina)
    {
        return new JsonObject
        {
            ["from"] = (pagina - 1) * tamanhoPagina,
            ["size"] = tamanhoPagina,
            ["track_total_hits"] = true,
            ["query"] = new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must"] = new JsonObject
                    {
                        ["multi_match"] = new JsonObject
                        {
                            ["query"] = texto,
                            ["fields"] = new JsonArray("titulo^3", "descricao"),
                            ["fuzziness"] = "AUTO",
                            ["prefix_length"] = 1,
                        },
                    },
                    ["should"] = new JsonArray(new JsonObject
                    {
                        ["match_phrase"] = new JsonObject
                        {
                            ["titulo"] = new JsonObject { ["query"] = texto, ["boost"] = 5 },
                        },
                    }),
                    ["filter"] = new JsonArray(new JsonObject
                    {
                        ["term"] = new JsonObject { ["status"] = _statusVisivel },
                    }),
                },
            },
            ["highlight"] = new JsonObject
            {
                ["encoder"] = "html",
                ["number_of_fragments"] = 1,
                ["fragment_size"] = 160,
                ["fields"] = new JsonObject { ["descricao"] = new JsonObject() },
            },
            ["sort"] = new JsonArray("_score", new JsonObject { ["dataFim"] = "asc" }),
        };
    }

    /// <summary>
    /// Converte a resposta de busca do Elasticsearch na página da aplicação.
    /// </summary>
    /// <param name="corpo">JSON da resposta.</param>
    /// <param name="pagina">Número da página pedida.</param>
    /// <param name="tamanhoPagina">Itens por página pedidos.</param>
    /// <returns>A página de resultados.</returns>
    private static Pagina<BuscaItemResponse> LerResposta(string corpo, int pagina, int tamanhoPagina)
    {
        using var documento = JsonDocument.Parse(corpo);
        var acertos = documento.RootElement.GetProperty("hits");
        var total = acertos.GetProperty("total").GetProperty("value").GetInt32();
        var itens = new List<BuscaItemResponse>();

        foreach (var acerto in acertos.GetProperty("hits").EnumerateArray())
        {
            var origem = acerto.GetProperty("_source");
            string? trecho = null;
            if (acerto.TryGetProperty("highlight", out var destaque) && destaque.TryGetProperty("descricao", out var fragmentos) && fragmentos.GetArrayLength() > 0)
            {
                trecho = fragmentos[0].GetString();
            }

            itens.Add(new BuscaItemResponse(
                Guid.Parse(acerto.GetProperty("_id").GetString()!),
                origem.GetProperty("titulo").GetString()!,
                trecho,
                acerto.GetProperty("_score").GetDouble(),
                DateOnly.ParseExact(origem.GetProperty("dataFim").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        return new Pagina<BuscaItemResponse>(itens, pagina, tamanhoPagina, total);
    }
}
