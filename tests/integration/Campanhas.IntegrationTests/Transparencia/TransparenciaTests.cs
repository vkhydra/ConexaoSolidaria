using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Transparencia;

/// <summary>
/// Testes de ponta a ponta do painel público de transparência: filtros, ordem, percentual, cache e cabeçalhos.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class TransparenciaTests(CampanhasApiFixture api) : IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Rota do painel.
    /// </summary>
    private const string _rota = "/api/v1/transparencia/campanhas";

    /// <summary>
    /// Data de hoje em São Paulo, como a API a calcula.
    /// </summary>
    private static readonly DateOnly _hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);

    /// <summary>
    /// Dadas campanhas ativa, concluída, cancelada, futura e vencida, quando um visitante consulta o painel, então só a ativa no período aparece, com cabeçalho de cache.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_SoMostraAtivasNoPeriodo_ComCabecalhoDeCache()
    {
        var prefixo = NovoPrefixo();
        using var gestor = Gestor();
        var ativa = await CriarAsync(gestor, $"{prefixo} ativa", 0, 30);
        var concluida = await CriarAsync(gestor, $"{prefixo} concluída", 0, 30);
        var cancelada = await CriarAsync(gestor, $"{prefixo} cancelada", 0, 30);
        await CriarAsync(gestor, $"{prefixo} futura", 10, 40);
        var vencida = await CriarAsync(gestor, $"{prefixo} vencida", 0, 30);
        (await gestor.PatchAsJsonAsync($"/api/v1/campanhas/{concluida}/status", new { status = "Concluida" }, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await gestor.PatchAsJsonAsync($"/api/v1/campanhas/{cancelada}/status", new { status = "Cancelada" }, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await api.ExecutarSqlAsync("UPDATE campanhas SET data_inicio = @i, data_fim = @f WHERE id = @id", ("i", _hoje.AddDays(-20)), ("f", _hoje.AddDays(-1)), ("id", vencida));
        await api.LimparCachePainelAsync();
        using var visitante = api.CreateClient();

        var resposta = await visitante.GetAsync(_rota, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        resposta.Headers.CacheControl!.Public.ShouldBeTrue();
        resposta.Headers.CacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(10));
        var painel = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var titulos = painel.EnumerateArray().Select(i => i.GetProperty("titulo").GetString()!).Where(t => t.StartsWith(prefixo, StringComparison.Ordinal)).ToList();
        titulos.ShouldBe([$"{prefixo} ativa"]);
        ativa.ShouldNotBe(Guid.Empty);
    }

    /// <summary>
    /// Dadas campanhas com datas fim diferentes, quando o painel é consultado, então vêm ordenadas pela data fim, com meta, total e percentual.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_OrdenaPelaDataFimECalculaPercentual()
    {
        var prefixo = NovoPrefixo();
        using var gestor = Gestor();
        await CriarAsync(gestor, $"{prefixo} B tarde", 0, 60);
        var cedo = await CriarAsync(gestor, $"{prefixo} A cedo", 0, 20);
        await api.ExecutarSqlAsync("UPDATE campanhas SET total_arrecadado = 150.00 WHERE id = @id", ("id", cedo));
        await api.LimparCachePainelAsync();
        using var visitante = api.CreateClient();

        var painel = await visitante.GetFromJsonAsync<JsonElement>(_rota, TestContext.Current.CancellationToken);

        var meus = painel.EnumerateArray().Where(i => i.GetProperty("titulo").GetString()!.StartsWith(prefixo, StringComparison.Ordinal)).ToList();
        meus.Select(i => i.GetProperty("titulo").GetString()).ShouldBe([$"{prefixo} A cedo", $"{prefixo} B tarde"]);
        meus[0].GetProperty("metaFinanceira").GetDecimal().ShouldBe(10000m);
        meus[0].GetProperty("totalArrecadado").GetDecimal().ShouldBe(150m);
        meus[0].GetProperty("percentualAtingido").GetDecimal().ShouldBe(1.50m);
        meus[0].GetProperty("dataFim").GetString().ShouldBe(_hoje.AddDays(20).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        meus[1].GetProperty("percentualAtingido").GetDecimal().ShouldBe(0m);
    }

    /// <summary>
    /// Dado o painel em cache, quando o total muda direto no banco, então a resposta continua igual até o cache ser removido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_UsaOCacheAteSerInvalidado()
    {
        var prefixo = NovoPrefixo();
        using var gestor = Gestor();
        var id = await CriarAsync(gestor, $"{prefixo} cache", 0, 30);
        await api.LimparCachePainelAsync();
        using var visitante = api.CreateClient();
        var antes = await TotalDeAsync(visitante, prefixo);

        await api.ExecutarSqlAsync("UPDATE campanhas SET total_arrecadado = 500.00 WHERE id = @id", ("id", id));
        var comCache = await TotalDeAsync(visitante, prefixo);
        await api.LimparCachePainelAsync();
        var semCache = await TotalDeAsync(visitante, prefixo);

        antes.ShouldBe(0m);
        comCache.ShouldBe(0m);
        semCache.ShouldBe(500m);
    }

    /// <summary>
    /// Dado o painel em cache, quando o gestor cria uma campanha pela API, então a nova campanha aparece logo, sem esperar a validade.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_CriarCampanha_InvalidaOCache()
    {
        var prefixo = NovoPrefixo();
        using var gestor = Gestor();
        using var visitante = api.CreateClient();
        await api.LimparCachePainelAsync();
        (await visitante.GetAsync(_rota, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        await CriarAsync(gestor, $"{prefixo} nova", 0, 30);
        var painel = await visitante.GetFromJsonAsync<JsonElement>(_rota, TestContext.Current.CancellationToken);

        painel.EnumerateArray().Select(i => i.GetProperty("titulo").GetString()).ShouldContain($"{prefixo} nova");
    }

    /// <summary>
    /// Dado um cabeçalho Authorization inválido, quando o painel público é consultado, então o cabeçalho é ignorado e a resposta é 200.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Painel_IgnoraCabecalhoAuthorization()
    {
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-que-nao-vale");

        var resposta = await cliente.GetAsync(_rota, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Lê o total arrecadado do painel para a campanha cujo título começa com o prefixo.
    /// </summary>
    /// <param name="cliente">Cliente HTTP.</param>
    /// <param name="prefixo">Prefixo do título.</param>
    /// <returns>O total arrecadado mostrado no painel.</returns>
    private static async Task<decimal> TotalDeAsync(HttpClient cliente, string prefixo)
    {
        var painel = await cliente.GetFromJsonAsync<JsonElement>(_rota, TestContext.Current.CancellationToken);
        return painel.EnumerateArray().Single(i => i.GetProperty("titulo").GetString()!.StartsWith(prefixo, StringComparison.Ordinal)).GetProperty("totalArrecadado").GetDecimal();
    }

    /// <summary>
    /// Gera um prefixo único para os títulos de um teste.
    /// </summary>
    /// <returns>Um prefixo curto.</returns>
    private static string NovoPrefixo()
    {
        return Guid.NewGuid().ToString("N")[..8];
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
    /// Cria uma campanha de 10000.00 e devolve o identificador.
    /// </summary>
    /// <param name="gestor">Cliente autenticado como gestor.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="inicioEmDias">Dias de hoje até a data início.</param>
    /// <param name="fimEmDias">Dias de hoje até a data fim.</param>
    /// <returns>O identificador da campanha criada.</returns>
    private static async Task<Guid> CriarAsync(HttpClient gestor, string titulo, int inicioEmDias, int fimEmDias)
    {
        var corpo = new { titulo, descricao = "d", dataInicio = _hoje.AddDays(inicioEmDias), dataFim = _hoje.AddDays(fimEmDias), metaFinanceira = 10000 };
        var resposta = await gestor.PostAsJsonAsync("/api/v1/campanhas", corpo, TestContext.Current.CancellationToken);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
    }
}
