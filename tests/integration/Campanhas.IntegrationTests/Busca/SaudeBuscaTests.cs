using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes do health check do Elasticsearch em <c>/health/ready</c>: <c>Healthy</c> com ele no ar e <c>Degraded</c> (sem derrubar
/// a prontidão) com ele fora. O estado geral do relatório não é conferido porque, nestes testes, o bus do RabbitMQ não é iniciado.
/// </summary>
/// <param name="comElasticsearch">API ligada ao Elasticsearch do container.</param>
/// <param name="semElasticsearch">API ligada a um Elasticsearch inalcançável.</param>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class SaudeBuscaTests(CampanhasApiComBuscaFixture comElasticsearch, CampanhasApiFixture semElasticsearch) : IClassFixture<CampanhasApiComBuscaFixture>, IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Dado o Elasticsearch no ar, quando a prontidão é consultada, então a verificação <c>elasticsearch</c> está <c>Healthy</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Prontidao_ElasticsearchNoAr_Healthy()
    {
        using var cliente = comElasticsearch.CreateClient();

        var resposta = await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        var relatorio = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        relatorio.GetProperty("verificacoes").GetProperty("elasticsearch").GetProperty("status").GetString().ShouldBe("Healthy");
    }

    /// <summary>
    /// Dado o Elasticsearch fora do ar, quando a prontidão é consultada, então a verificação é <c>Degraded</c> (e não <c>Unhealthy</c>, que tiraria a API do balanceamento).
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Prontidao_ElasticsearchForaDoAr_DegradedSemDerrubarAApi()
    {
        using var cliente = semElasticsearch.CreateClient();

        var resposta = await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        var relatorio = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        relatorio.GetProperty("verificacoes").GetProperty("elasticsearch").GetProperty("status").GetString().ShouldBe("Degraded");
    }
}
