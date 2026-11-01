using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes da busca com o Elasticsearch fora do ar: só a busca falha (503), o painel de transparência segue respondendo.
/// </summary>
/// <param name="api">API configurada com um endereço de Elasticsearch inalcançável.</param>
[Trait("Category", "Integration")]
public sealed class BuscaIndisponivelTests(CampanhasApiFixture api) : IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Dado o Elasticsearch fora do ar, quando um visitante busca, então recebe 503 com o código <c>busca.indisponivel</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_ElasticsearchForaDoAr_Recebe503()
    {
        using var visitante = api.CreateClient();

        var resposta = await visitante.GetAsync("/api/v1/campanhas/busca?q=natal", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        problema.GetProperty("code").GetString().ShouldBe("busca.indisponivel");
    }

    /// <summary>
    /// Dado o Elasticsearch fora do ar, quando um visitante consulta o painel de transparência, então ele continua respondendo 200.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Painel_ElasticsearchForaDoAr_ContinuaRespondendo()
    {
        using var visitante = api.CreateClient();

        var resposta = await visitante.GetAsync("/api/v1/transparencia/campanhas", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
