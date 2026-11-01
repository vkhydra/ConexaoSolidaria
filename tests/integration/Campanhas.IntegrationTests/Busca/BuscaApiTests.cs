using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes de ponta a ponta do endpoint público <c>GET /api/v1/campanhas/busca</c> com a API inteira e um Elasticsearch real.
/// </summary>
/// <param name="api">API ligada ao Elasticsearch do container.</param>
/// <param name="elasticsearch">Elasticsearch compartilhado pela coleção.</param>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class BuscaApiTests(CampanhasApiComBuscaFixture api, ElasticsearchFixture elasticsearch) : IClassFixture<CampanhasApiComBuscaFixture>
{
    /// <summary>
    /// Rota da busca.
    /// </summary>
    private const string _rota = "/api/v1/campanhas/busca";

    /// <summary>
    /// Dada uma campanha indexada, quando um visitante sem token busca com erro de digitação, então recebe 200 com o item e o destaque.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_VisitanteComErroDeDigitacao_Recebe200ComItem()
    {
        var id = await IndexarAsync("Natal Solidário", "Presentes e ceia para 120 crianças");
        using var visitante = api.CreateClient();

        var resposta = await visitante.GetAsync($"{_rota}?q=natl%20solidaro&page=1&pageSize=10", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var item = corpo.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == id);
        item.GetProperty("titulo").GetString().ShouldBe("Natal Solidário");
        item.GetProperty("dataFim").GetString().ShouldBe("2026-12-24");
        corpo.GetProperty("page").GetInt32().ShouldBe(1);
        corpo.GetProperty("totalPages").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Dado um pedido sem <c>q</c>, com <c>q</c> curto ou com paginação fora dos limites, quando enviado, então recebe 400 com o campo.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_PedidoInvalido_Recebe400ComCampo()
    {
        using var visitante = api.CreateClient();

        foreach (var (consulta, campo) in new[] { ("", "q"), ("?q=a", "q"), ("?q=natal&page=0", "page"), ("?q=natal&pageSize=51", "pageSize") })
        {
            var resposta = await visitante.GetAsync($"{_rota}{consulta}", TestContext.Current.CancellationToken);

            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest, consulta);
            var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue(consulta);
        }
    }

    /// <summary>
    /// Dado o endpoint de gestão de campanhas, quando um visitante sem token o acessa, então continua recebendo 401 (a busca pública não abre as demais rotas).
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Gestao_VisitanteSemToken_Recebe401()
    {
        using var visitante = api.CreateClient();

        (await visitante.GetAsync("/api/v1/campanhas", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await visitante.GetAsync($"/api/v1/campanhas/{Guid.NewGuid()}", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Dado um gestor autenticado, quando busca, então também recebe 200 (o endpoint é público para qualquer perfil).
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Buscar_GestorAutenticado_Recebe200()
    {
        using var gestor = api.CreateClient();
        gestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken("GestorONG"));

        var resposta = await gestor.GetAsync($"{_rota}?q=natal", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Grava uma campanha ativa direto no índice e a deixa pesquisável.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="descricao">Descrição da campanha.</param>
    /// <returns>O identificador da campanha indexada.</returns>
    private async Task<Guid> IndexarAsync(string titulo, string descricao)
    {
        var ct = TestContext.Current.CancellationToken;
        var id = Guid.NewGuid();
        await api.Services.GetRequiredService<IIndiceCampanhas>().GravarAsync(new CampanhaIndexada(id, titulo, descricao, "Ativa", 5000m, new DateOnly(2026, 12, 24), 1), ct);
        await elasticsearch.Cliente.Indices.RefreshAsync(MapeamentoIndice.NomeAlias, ct);
        return id;
    }
}
