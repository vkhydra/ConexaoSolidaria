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
    /// Opções para ler o envelope do MassTransit, que grava o <c>decimal</c> como texto.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoesDoEnvelope = new(JsonSerializerDefaults.Web) { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };

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
    /// Dada uma campanha criada e depois cancelada pela API, quando os eventos do Outbox são entregues ao índice na ordem, então
    /// ela aparece na busca após a criação e some após o cancelamento.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_CampanhaCriadaECanceladaPelaApi_AparecePoisSome()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefixo = Guid.NewGuid().ToString("N")[..8];
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);
        using var gestor = api.CreateClient();
        gestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken("GestorONG"));
        using var visitante = api.CreateClient();
        var criada = await gestor.PostAsJsonAsync("/api/v1/campanhas", new { titulo = $"{prefixo} Páscoa Feliz", descricao = "Chocolates", dataInicio = hoje, dataFim = hoje.AddDays(30), metaFinanceira = 5000 }, ct);
        var id = (await criada.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();

        await EntregarEventosAsync(id, 1);
        var aposCriar = await visitante.GetFromJsonAsync<JsonElement>($"{_rota}?q={prefixo}%20pascoa", ct);
        (await gestor.PatchAsJsonAsync($"/api/v1/campanhas/{id}/status", new { status = "Cancelada" }, ct)).EnsureSuccessStatusCode();
        await EntregarEventosAsync(id, 2);
        var aposCancelar = await visitante.GetFromJsonAsync<JsonElement>($"{_rota}?q={prefixo}%20pascoa", ct);

        aposCriar.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ShouldContain(id);
        aposCancelar.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ShouldNotContain(id);
    }

    /// <summary>
    /// Entrega ao índice os primeiros eventos do Outbox da campanha, como o consumidor faria ao receber da fila.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="quantidade">Quantidade de eventos já gravados no Outbox que devem ser entregues.</param>
    /// <returns>Tarefa que termina com os eventos gravados no índice e visíveis à busca.</returns>
    private async Task EntregarEventosAsync(Guid id, int quantidade)
    {
        var ct = TestContext.Current.CancellationToken;
        var corpos = await api.LerOutboxAsync(id);
        corpos.Count.ShouldBe(quantidade);
        var evento = JsonSerializer.Deserialize<EnvelopeDeEvento>(corpos[^1], _opcoesDoEnvelope)!.Message;
        await api.Services.GetRequiredService<IIndiceCampanhas>().GravarAsync(CampanhaIndexada.De(evento), ct);
        await elasticsearch.Cliente.Indices.RefreshAsync(MapeamentoIndice.NomeAlias, ct);
    }

    /// <summary>
    /// Envelope do MassTransit gravado no Outbox, com a mensagem de alteração da campanha.
    /// </summary>
    /// <param name="Message">Evento publicado.</param>
    private sealed record EnvelopeDeEvento(ConexaoSolidaria.Contracts.V1.CampanhaAlteradaEvent Message);

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
