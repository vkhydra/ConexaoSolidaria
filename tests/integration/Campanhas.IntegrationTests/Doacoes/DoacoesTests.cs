using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.Application.Doacoes;
using Campanhas.IntegrationTests.Suporte;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.DependencyInjection;

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
    /// Dado um pedido fora do formato (valor zero, três casas, acima do limite ou campanha mal formada), quando enviado, então
    /// recebe 400 e nada vai para o Outbox.
    /// </summary>
    /// <param name="corpo">Corpo JSON enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("""{"idCampanha":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","valorDoacao":0}""")]
    [InlineData("""{"idCampanha":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","valorDoacao":10.005}""")]
    [InlineData("""{"idCampanha":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","valorDoacao":1000000.01}""")]
    [InlineData("""{"idCampanha":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f"}""")]
    [InlineData("""{"idCampanha":"nao-e-um-guid","valorDoacao":10}""")]
    [InlineData("""{}""")]
    public async Task Receber_PedidoInvalido_Devolve400(string corpo)
    {
        using var doador = Cliente("Doador");
        using var conteudo = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json");

        var resposta = await doador.PostAsync(_rota, conteudo, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Dado um pedido sem valor, quando enviado, então o erro 400 aponta o campo valorDoacao.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ValorAusente_Devolve400ComOCampo()
    {
        using var doador = Cliente("Doador");

        var resposta = await doador.PostAsJsonAsync(_rota, new { idCampanha = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var erro = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        erro.GetProperty("code").GetString().ShouldBe("validacao");
        erro.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["valorDoacao"]);
    }

    /// <summary>
    /// Dado um chamador sem token ou com papel de gestor, quando doa, então recebe 401 ou 403.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_SemTokenOuPapelErrado_Devolve401Ou403()
    {
        using var anonimo = api.CreateClient();
        using var gestor = Cliente("GestorONG");
        var corpo = new { idCampanha = Guid.NewGuid(), valorDoacao = 10m };

        (await anonimo.PostAsJsonAsync(_rota, corpo, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await gestor.PostAsJsonAsync(_rota, corpo, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando o doador doa, então recebe 404 campanha.nao_encontrada e nada vai para o Outbox.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CampanhaInexistente_Devolve404()
    {
        using var doador = Cliente("Doador");
        var campanhaId = Guid.NewGuid();

        var resposta = await doador.PostAsJsonAsync(_rota, new { idCampanha = campanhaId, valorDoacao = 10m }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.nao_encontrada");
        (await api.LerOutboxAsync(campanhaId)).ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma campanha concluída, cancelada ou fora do período, quando o doador doa, então recebe 422
    /// doacao.campanha_nao_ativa e nenhum evento de doação vai para o Outbox.
    /// </summary>
    /// <param name="situacao">Situação que torna a campanha inapta.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("Concluida")]
    [InlineData("Cancelada")]
    [InlineData("Encerrada")]
    public async Task Receber_CampanhaInapta_Devolve422SemOutbox(string situacao)
    {
        var campanhaId = await CriarCampanhaAsync();
        if (situacao == "Encerrada")
        {
            await api.ExecutarSqlAsync("UPDATE campanhas SET data_inicio = data_inicio - 40, data_fim = data_fim - 40 WHERE id = @id", ("id", campanhaId));
        }
        else
        {
            using var gestor = Cliente("GestorONG");
            (await gestor.PatchAsJsonAsync($"/api/v1/campanhas/{campanhaId}/status", new { status = situacao }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        using var doador = Cliente("Doador");
        var resposta = await doador.PostAsJsonAsync(_rota, new { idCampanha = campanhaId, valorDoacao = 10m }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("doacao.campanha_nao_ativa");
        (await api.LerOutboxAsync(campanhaId)).Count(m => m.Contains("doacaoId", StringComparison.Ordinal)).ShouldBe(0);
    }

    /// <summary>
    /// Dada a mesma Idempotency-Key com o mesmo corpo, quando o doador repete o pedido, então recebe o mesmo doacaoId
    /// e só um evento fica no Outbox.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveRepetidaComMesmoCorpo_DevolveAMesmaDoacaoSemNovoEvento()
    {
        var campanhaId = await CriarCampanhaAsync();
        using var doador = Cliente("Doador");
        var chave = Guid.NewGuid().ToString();

        var primeira = await Doar(doador, campanhaId, 150.00m, chave);
        var segunda = await Doar(doador, campanhaId, 150.00m, chave);

        primeira.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        segunda.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var idPrimeira = (await primeira.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("doacaoId").GetGuid();
        var idSegunda = (await segunda.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("doacaoId").GetGuid();
        idSegunda.ShouldBe(idPrimeira);
        (await api.LerOutboxAsync(campanhaId)).Count(m => m.Contains("doacaoId", StringComparison.Ordinal)).ShouldBe(1);
    }

    /// <summary>
    /// Dada a mesma Idempotency-Key com outro valor, quando o doador envia, então recebe 422 doacao.chave_idempotencia_conflitante.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveRepetidaComOutroCorpo_Devolve422()
    {
        var campanhaId = await CriarCampanhaAsync();
        using var doador = Cliente("Doador");
        var chave = Guid.NewGuid().ToString();
        (await Doar(doador, campanhaId, 150.00m, chave)).StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var resposta = await Doar(doador, campanhaId, 151.00m, chave);

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("doacao.chave_idempotencia_conflitante");
    }

    /// <summary>
    /// Dada a mesma Idempotency-Key usada por dois doadores diferentes, quando ambos doam, então cada um recebe a sua doação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_MesmaChaveDeDoadoresDiferentes_NaoSeMisturam()
    {
        var campanhaId = await CriarCampanhaAsync();
        using var ana = Cliente("Doador");
        using var bia = Cliente("Doador");
        var chave = Guid.NewGuid().ToString();

        var daAna = await (await Doar(ana, campanhaId, 10m, chave)).Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var daBia = await (await Doar(bia, campanhaId, 10m, chave)).Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        daBia.GetProperty("doacaoId").GetGuid().ShouldNotBe(daAna.GetProperty("doacaoId").GetGuid());
    }

    /// <summary>
    /// Dada uma Idempotency-Key com mais de 100 caracteres, quando o doador envia, então recebe 400 validacao.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveLongaDemais_Devolve400()
    {
        var campanhaId = await CriarCampanhaAsync();
        using var doador = Cliente("Doador");

        var resposta = await Doar(doador, campanhaId, 10m, new string('c', 101));

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["Idempotency-Key"]);
    }

    /// <summary>
    /// Dadas duas gravações da mesma chave, quando a segunda chega ao banco, então o publicador lança
    /// <see cref="ChaveJaRegistradaException"/> e nenhum evento novo é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Publicador_ChaveDuplicadaNoBanco_LancaChaveJaRegistrada()
    {
        var campanhaId = await CriarCampanhaAsync();
        var doadorId = Guid.NewGuid();
        var chave = new ChaveParaRegistrar(Guid.NewGuid().ToString(), new byte[32]);
        var evento = new DoacaoRecebidaEvent(Guid.NewGuid(), campanhaId, doadorId, 10m, DateTimeOffset.UtcNow);
        await PublicarEmNovoEscopoAsync(evento, chave);
        var repetido = evento with { DoacaoId = Guid.NewGuid() };

        await Should.ThrowAsync<ChaveJaRegistradaException>(() => PublicarEmNovoEscopoAsync(repetido, chave));

        (await api.LerOutboxAsync(campanhaId)).Count(m => m.Contains(repetido.DoacaoId.ToString(), StringComparison.Ordinal)).ShouldBe(0);
    }

    /// <summary>
    /// Dadas chaves de 25 horas, de 23 horas e de agora, quando a limpeza roda, então só a de 25 horas é removida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Limpeza_RemoveSoChavesExpiradas()
    {
        var doadorId = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;
        foreach (var (chave, idade) in new[] { ("velha", 25), ("meia", 23), ("nova", 0) })
        {
            await api.ExecutarSqlAsync(
                "INSERT INTO chaves_idempotencia (chave, doador_id, doacao_id, hash_corpo, criada_em) VALUES (@chave, @doador, @doacao, @hash, @criada)",
                ("chave", $"{doadorId}-{chave}"),
                ("doador", doadorId),
                ("doacao", Guid.NewGuid()),
                ("hash", new byte[32]),
                ("criada", agora.AddHours(-idade)));
        }

        await using var escopo = api.Services.CreateAsyncScope();
        var chaves = escopo.ServiceProvider.GetRequiredService<IChavesIdempotencia>();

        var removidas = await chaves.RemoverExpiradasAsync(agora.AddHours(-24), TestContext.Current.CancellationToken);

        removidas.ShouldBeGreaterThanOrEqualTo(1);
        (await chaves.ObterAsync($"{doadorId}-velha", doadorId, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await chaves.ObterAsync($"{doadorId}-meia", doadorId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await chaves.ObterAsync($"{doadorId}-nova", doadorId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    /// <summary>
    /// Envia uma doação com a chave de idempotência informada.
    /// </summary>
    /// <param name="cliente">Cliente autenticado como doador.</param>
    /// <param name="campanhaId">Campanha da doação.</param>
    /// <param name="valor">Valor da doação.</param>
    /// <param name="chave">Valor do cabeçalho <c>Idempotency-Key</c>.</param>
    /// <returns>A resposta HTTP.</returns>
    private static async Task<HttpResponseMessage> Doar(HttpClient cliente, Guid campanhaId, decimal valor, string chave)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, _rota) { Content = JsonContent.Create(new { idCampanha = campanhaId, valorDoacao = valor }) };
        pedido.Headers.Add("Idempotency-Key", chave);
        return await cliente.SendAsync(pedido, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Publica um evento por um publicador resolvido em um escopo novo, como cada requisição faz.
    /// </summary>
    /// <param name="evento">Evento da doação.</param>
    /// <param name="chave">Chave de idempotência.</param>
    /// <returns>Tarefa que termina com o evento gravado.</returns>
    private async Task PublicarEmNovoEscopoAsync(DoacaoRecebidaEvent evento, ChaveParaRegistrar chave)
    {
        await using var escopo = api.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IPublicadorDeDoacoes>().PublicarRecebidaAsync(evento, chave, TestContext.Current.CancellationToken);
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
