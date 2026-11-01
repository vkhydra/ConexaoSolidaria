using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes de ponta a ponta de <c>POST /api/v1/admin/reindexar</c>: PostgreSQL e Elasticsearch reais, troca do alias,
/// autorização e reindexação já em andamento.
/// </summary>
/// <param name="api">API ligada ao Elasticsearch do container.</param>
/// <param name="elasticsearch">Elasticsearch compartilhado pela coleção.</param>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class ReindexacaoTests(CampanhasApiComBuscaFixture api, ElasticsearchFixture elasticsearch) : IClassFixture<CampanhasApiComBuscaFixture>
{
    /// <summary>
    /// Rota da reindexação.
    /// </summary>
    private const string _rota = "/api/v1/admin/reindexar";

    /// <summary>
    /// Dadas campanhas ativa e cancelada no banco e um índice vazio, quando o gestor reindexa, então recebe 202, a ativa fica pesquisável,
    /// a cancelada não aparece e o alias passa a apontar para um único índice novo.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Reindexar_GestorComCampanhasNoBanco_RecriaOIndiceETrocaOAlias()
    {
        var ct = TestContext.Current.CancellationToken;
        await elasticsearch.Cliente.Indices.DeleteAsync(Indices.Parse("campanhas-*"), ct);
        await MapeamentoIndice.GarantirAliasAsync(elasticsearch.Cliente, ct);
        var prefixo = Guid.NewGuid().ToString("N")[..8];
        using var gestor = Gestor();
        var ativa = await CriarAsync(gestor, $"{prefixo} lanche solidário");
        var cancelada = await CriarAsync(gestor, $"{prefixo} lanche cancelado");
        (await gestor.PatchAsJsonAsync($"/api/v1/campanhas/{cancelada}/status", new { status = "Cancelada" }, ct)).EnsureSuccessStatusCode();

        var resposta = await gestor.PostAsync(_rota, null, ct);
        await api.Services.GetRequiredService<Reindexar>().Execucao!;

        resposta.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var alias = await elasticsearch.Cliente.Indices.GetAliasAsync(g => g.Name(MapeamentoIndice.NomeAlias), ct);
        var indices = alias.Aliases!.Keys.Select(k => k.ToString()).ToList();
        indices.Count.ShouldBe(1);
        indices[0].ShouldNotBe(MapeamentoIndice.NomeIndiceInicial);
        using var visitante = api.CreateClient();
        var busca = await visitante.GetFromJsonAsync<JsonElement>($"/api/v1/campanhas/busca?q={prefixo}%20lanche", ct);
        var achadas = busca.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToList();
        achadas.ShouldContain(ativa);
        achadas.ShouldNotContain(cancelada);
    }

    /// <summary>
    /// Dado um visitante sem token ou um doador, quando pede a reindexação, então recebe 401 ou 403.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Reindexar_SemPermissao_Recebe401Ou403()
    {
        using var visitante = api.CreateClient();
        using var doador = api.CreateClient();
        doador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken("Doador"));

        (await visitante.PostAsync(_rota, null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await doador.PostAsync(_rota, null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Dada uma reindexação ainda em andamento, quando o gestor pede outra, então recebe 409 <c>busca.reindexacao_em_andamento</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Reindexar_JaEmAndamento_Recebe409()
    {
        var liberar = new TaskCompletionSource();
        var gerenciador = Substitute.For<IGerenciadorDeIndice>();
        gerenciador.CriarIndiceNovoAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await liberar.Task;
            return "campanhas-v-lenta";
        });
        await using var fabrica = api.WithWebHostBuilder(builder => builder.ConfigureServices(servicos =>
        {
            servicos.AddSingleton(gerenciador);
            servicos.AddSingleton<IFonteDeCampanhas>(Substitute.For<IFonteDeCampanhas>());
        }));
        using var gestor = fabrica.CreateClient();
        gestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken("GestorONG"));

        var primeira = await gestor.PostAsync(_rota, null, TestContext.Current.CancellationToken);
        var segunda = await gestor.PostAsync(_rota, null, TestContext.Current.CancellationToken);
        liberar.SetResult();

        primeira.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        segunda.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problema = await segunda.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        problema.GetProperty("code").GetString().ShouldBe("busca.reindexacao_em_andamento");
        await fabrica.Services.GetRequiredService<Reindexar>().Execucao!;
    }

    /// <summary>
    /// Dadas cinco campanhas no banco, quando a fonte lê em lotes de dois, então todas chegam, em ordem de identificador, com versão positiva.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Fonte_LerEmLotes_EntregaTodasEmLotesDoTamanhoPedido()
    {
        var prefixo = Guid.NewGuid().ToString("N")[..8];
        using var gestor = Gestor();
        var criadas = new List<Guid>();
        for (var n = 1; n <= 5; n++)
        {
            criadas.Add(await CriarAsync(gestor, $"{prefixo} campanha {n}"));
        }

        var lotes = new List<IReadOnlyList<CampanhaIndexada>>();
        await foreach (var lote in api.Services.GetRequiredService<IFonteDeCampanhas>().LerEmLotesAsync(2, TestContext.Current.CancellationToken))
        {
            lotes.Add(lote);
        }

        lotes.ShouldAllBe(l => l.Count <= 2);
        var todas = lotes.SelectMany(l => l).ToList();
        todas.Select(c => c.Id).ShouldBe(todas.Select(c => c.Id).Order());
        todas.Where(c => criadas.Contains(c.Id)).Count().ShouldBe(5);
        todas.ShouldAllBe(c => c.Versao > 0);
        todas.First(c => c.Id == criadas[0]).DataFim.ShouldBeGreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)));
    }

    /// <summary>
    /// Dado um tamanho de lote inválido, quando a leitura começa, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Fonte_LoteInvalido_LancaExcecao()
    {
        var fonte = api.Services.GetRequiredService<IFonteDeCampanhas>();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var lote in fonte.LerEmLotesAsync(0, TestContext.Current.CancellationToken))
            {
                lote.ShouldBeEmpty();
            }
        });
    }

    /// <summary>
    /// Cria um cliente HTTP autenticado como gestor.
    /// </summary>
    /// <returns>O cliente.</returns>
    private HttpClient Gestor()
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken("GestorONG"));
        return cliente;
    }

    /// <summary>
    /// Cria uma campanha ativa por 30 dias a partir de hoje e devolve o identificador.
    /// </summary>
    /// <param name="gestor">Cliente autenticado como gestor.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>O identificador da campanha criada.</returns>
    private static async Task<Guid> CriarAsync(HttpClient gestor, string titulo)
    {
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);
        var corpo = new { titulo, descricao = "Lanche para quem precisa", dataInicio = hoje, dataFim = hoje.AddDays(30), metaFinanceira = 10000 };
        var resposta = await gestor.PostAsJsonAsync("/api/v1/campanhas", corpo, TestContext.Current.CancellationToken);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
    }
}
