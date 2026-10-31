using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Doacoes;

/// <summary>
/// Testes de ponta a ponta do recebimento de doação: API real, PostgreSQL em container, JWT verdadeiro e Outbox conferido.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class DoacoesTests(CampanhasApiFixture api) : IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Rota base das doações.
    /// </summary>
    private const string _rota = "/api/v1/doacoes";

    /// <summary>
    /// Data de hoje em São Paulo, como a API a calcula.
    /// </summary>
    private static readonly DateOnly _hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);

    /// <summary>
    /// Dada uma campanha ativa, quando o doador doa, então recebe 202, o evento fica no Outbox com o doador do token
    /// e o total da campanha continua zero.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CampanhaAtiva_Devolve202ComEventoNoOutbox()
    {
        var campanhaId = await CriarCampanhaAsync();
        var doadorId = Guid.NewGuid();
        using var doador = Cliente("Doador", doadorId);

        var resposta = await doador.PostAsJsonAsync(_rota, new { idCampanha = campanhaId, valorDoacao = 150.00m }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var doacaoId = corpo.GetProperty("doacaoId").GetGuid();
        corpo.GetProperty("status").GetString().ShouldBe("Recebida");

        var outbox = await api.LerOutboxAsync(campanhaId);
        var evento = outbox.Select(m => JsonDocument.Parse(m).RootElement.GetProperty("message")).Single(m => m.TryGetProperty("doacaoId", out _));
        evento.GetProperty("doacaoId").GetGuid().ShouldBe(doacaoId);
        evento.GetProperty("doadorId").GetGuid().ShouldBe(doadorId);
        decimal.Parse(evento.GetProperty("valor").GetString()!, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(150.00m);

        using var gestor = Cliente("GestorONG");
        var campanha = await gestor.GetFromJsonAsync<JsonElement>($"/api/v1/campanhas/{campanhaId}", TestContext.Current.CancellationToken);
        campanha.GetProperty("totalArrecadado").GetDecimal().ShouldBe(0m);
    }

    /// <summary>
    /// Cria um cliente HTTP autenticado com o papel informado.
    /// </summary>
    /// <param name="role">Papel do token.</param>
    /// <param name="usuarioId">Identificador do usuário no token; sorteado quando não informado.</param>
    /// <returns>O cliente com o cabeçalho <c>Authorization</c>.</returns>
    private HttpClient Cliente(string role, Guid? usuarioId = null)
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken(role, usuarioId));
        return cliente;
    }

    /// <summary>
    /// Cria uma campanha ativa pela API, como gestor.
    /// </summary>
    /// <returns>O identificador da campanha criada.</returns>
    private async Task<Guid> CriarCampanhaAsync()
    {
        using var gestor = Cliente("GestorONG");
        var resposta = await gestor.PostAsJsonAsync(
            "/api/v1/campanhas",
            new { titulo = "Natal Solidário", descricao = "Presentes e ceia.", dataInicio = _hoje, dataFim = _hoje.AddDays(30), metaFinanceira = 10000 },
            TestContext.Current.CancellationToken);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
    }
}
