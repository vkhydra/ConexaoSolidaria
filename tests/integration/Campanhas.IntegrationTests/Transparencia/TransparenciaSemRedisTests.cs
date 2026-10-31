using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Transparencia;

/// <summary>
/// Teste da resiliência do painel: com o Redis fora do ar, o painel continua respondendo com os dados do banco.
/// Fica numa classe própria porque derruba o Redis da fixture.
/// </summary>
/// <param name="api">API com banco e Redis exclusivos desta classe.</param>
[Trait("Category", "Integration")]
public sealed class TransparenciaSemRedisTests(CampanhasApiFixture api) : IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Dado o Redis fora do ar, quando um visitante consulta o painel, então recebe 200 com os dados do banco; a criação de campanha também segue funcionando.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_RedisForaDoAr_Devolve200ComDadosDoBanco()
    {
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);
        using var gestor = api.CreateClient();
        gestor.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api.GerarToken("GestorONG"));
        var corpo = new { titulo = "Sem Redis", descricao = "d", dataInicio = hoje, dataFim = hoje.AddDays(30), metaFinanceira = 1000 };
        (await gestor.PostAsJsonAsync("/api/v1/campanhas", corpo, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);
        await api.LimparCachePainelAsync();
        await api.PararRedisAsync();
        using var visitante = api.CreateClient();

        var resposta = await visitante.GetAsync("/api/v1/transparencia/campanhas", TestContext.Current.CancellationToken);
        var criada = await gestor.PostAsJsonAsync("/api/v1/campanhas", corpo, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var painel = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        painel.EnumerateArray().Select(i => i.GetProperty("titulo").GetString()).ShouldContain("Sem Redis");
        criada.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
