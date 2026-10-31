using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.IntegrationTests.Suporte;

namespace Campanhas.IntegrationTests.Campanhas;

/// <summary>
/// Testes de ponta a ponta da gestão de campanhas: API real, PostgreSQL em container, JWT verdadeiro e Outbox conferido.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class CampanhasEndpointsTests(CampanhasApiFixture api) : IClassFixture<CampanhasApiFixture>
{
    /// <summary>
    /// Rota base das campanhas.
    /// </summary>
    private const string _rota = "/api/v1/campanhas";

    /// <summary>
    /// Data de hoje em São Paulo, como a API a calcula.
    /// </summary>
    private static readonly DateOnly _hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);

    /// <summary>
    /// Dados válidos, quando o gestor cria a campanha, então recebe 201 com Location e ETag, e o evento fica no Outbox.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Criar_DadosValidos_Devolve201ComEventoNoOutbox()
    {
        using var gestor = Gestor();

        var resposta = await gestor.PostAsJsonAsync(_rota, Corpo("Natal Solidário"), TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var campanha = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var id = campanha.GetProperty("id").GetGuid();
        resposta.Headers.Location!.OriginalString.ShouldBe($"/api/v1/campanhas/{id}");
        resposta.Headers.ETag.ShouldNotBeNull();
        campanha.GetProperty("status").GetString().ShouldBe("Ativa");
        campanha.GetProperty("totalArrecadado").GetDecimal().ShouldBe(0m);
        campanha.GetProperty("percentualAtingido").GetDecimal().ShouldBe(0m);
        campanha.GetProperty("metaFinanceira").GetDecimal().ShouldBe(10000m);
        campanha.TryGetProperty("versao", out _).ShouldBeFalse();

        var outbox = await api.LerOutboxAsync(id);
        var evento = JsonDocument.Parse(outbox.ShouldHaveSingleItem()).RootElement.GetProperty("message");
        evento.GetProperty("titulo").GetString().ShouldBe("Natal Solidário");
        evento.GetProperty("status").GetString().ShouldBe("Ativa");
    }

    /// <summary>
    /// Dado um pedido inválido (título vazio, meta com 3 casas), quando enviado, então recebe 400 validacao com o campo certo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Criar_FormatoInvalido_Devolve400()
    {
        using var gestor = Gestor();

        var resposta = await gestor.PostAsJsonAsync(_rota, new { titulo = string.Empty, descricao = "d", dataInicio = _hoje, dataFim = _hoje, metaFinanceira = 10.123 }, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var erro = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        erro.GetProperty("code").GetString().ShouldBe("validacao");
        erro.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["titulo", "metaFinanceira"], ignoreOrder: true);
    }

    /// <summary>
    /// Dadas regras de negócio violadas, quando enviadas, então recebe 422 com o código estável.
    /// </summary>
    /// <param name="inicioDias">Dias somados a hoje para a data início.</param>
    /// <param name="fimDias">Dias somados a hoje para a data fim.</param>
    /// <param name="meta">Meta enviada.</param>
    /// <param name="codigo">Código esperado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(-10, -1, 1000, "campanha.data_fim_passada")]
    [InlineData(0, 30, 0, "campanha.meta_invalida")]
    [InlineData(0, 30, -10, "campanha.meta_invalida")]
    [InlineData(10, 1, 1000, "campanha.periodo_invalido")]
    public async Task Criar_RegraDeNegocioViolada_Devolve422(int inicioDias, int fimDias, int meta, string codigo)
    {
        using var gestor = Gestor();
        var corpo = new { titulo = "Campanha", descricao = "d", dataInicio = _hoje.AddDays(inicioDias), dataFim = _hoje.AddDays(fimDias), metaFinanceira = meta };

        var resposta = await gestor.PostAsJsonAsync(_rota, corpo, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe(codigo);
    }

    /// <summary>
    /// Dado ninguém autenticado ou um doador, quando acessa as rotas de gestão, então recebe 401 e 403.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Rotas_SemTokenOuComDoador_Devolvem401E403()
    {
        using var anonimo = api.CreateClient();
        using var doador = Cliente("Doador");

        (await anonimo.GetAsync(_rota, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonimo.PostAsJsonAsync(_rota, Corpo("x"), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await doador.PostAsJsonAsync(_rota, Corpo("Natal"), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await doador.GetAsync(_rota, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await doador.GetAsync($"{_rota}/{Guid.NewGuid()}", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await doador.PutAsJsonAsync($"{_rota}/{Guid.NewGuid()}", Corpo("x"), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await doador.PatchAsJsonAsync($"{_rota}/{Guid.NewGuid()}/status", new { status = "Concluida" }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Dada uma campanha criada, quando detalhada, então recebe 200 com ETag; um identificador inexistente recebe 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Detalhar_ExistenteEInexistente_Devolve200E404()
    {
        using var gestor = Gestor();
        var criada = await CriarAsync(gestor, "Detalhe");

        var existente = await gestor.GetAsync($"{_rota}/{criada.Id}", TestContext.Current.CancellationToken);
        var inexistente = await gestor.GetAsync($"{_rota}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        existente.StatusCode.ShouldBe(HttpStatusCode.OK);
        existente.Headers.ETag!.Tag.ShouldBe(criada.ETag);
        inexistente.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await inexistente.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.nao_encontrada");
    }

    /// <summary>
    /// Dadas campanhas criadas, quando listadas com filtro e paginação, então recebe 200 paginado; parâmetros inválidos recebem 400.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Listar_ComFiltroEPaginacao_Devolve200Paginado()
    {
        using var gestor = Gestor();
        await CriarAsync(gestor, "Lista A");
        await CriarAsync(gestor, "Lista B");

        var pagina = await gestor.GetAsync($"{_rota}?status=Ativa&page=1&pageSize=1", TestContext.Current.CancellationToken);
        var invalida = await gestor.GetAsync($"{_rota}?page=0&pageSize=101", TestContext.Current.CancellationToken);
        var padrao = await gestor.GetAsync(_rota, TestContext.Current.CancellationToken);

        pagina.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await pagina.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("items").GetArrayLength().ShouldBe(1);
        corpo.GetProperty("page").GetInt32().ShouldBe(1);
        corpo.GetProperty("pageSize").GetInt32().ShouldBe(1);
        corpo.GetProperty("totalItems").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
        corpo.GetProperty("totalPages").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
        invalida.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalida.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["page", "pageSize"], ignoreOrder: true);
        padrao.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dada a versão certa no If-Match, quando a meta é alterada, então recebe 204 com novo ETag, a mudança é gravada e um segundo evento entra no Outbox.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Editar_ComVersaoCerta_Devolve204ENovoEtag()
    {
        using var gestor = Gestor();
        var criada = await CriarAsync(gestor, "Editar");

        var resposta = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Editar", meta: 12000), criada.ETag);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        resposta.Headers.ETag!.Tag.ShouldNotBe(criada.ETag);
        var detalhe = await gestor.GetFromJsonAsync<JsonElement>($"{_rota}/{criada.Id}", TestContext.Current.CancellationToken);
        detalhe.GetProperty("metaFinanceira").GetDecimal().ShouldBe(12000m);
        (await api.LerOutboxAsync(criada.Id)).Count.ShouldBe(2);
    }

    /// <summary>
    /// Dado um PUT sem If-Match, com versão antiga ou de campanha não editável, quando enviado, então recebe 412, 409 e 422.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Editar_SemVersaoVersaoAntigaOuNaoEditavel_Devolve412409E422()
    {
        using var gestor = Gestor();
        var criada = await CriarAsync(gestor, "Conflito");
        (await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Conflito", meta: 11000), criada.ETag)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var semVersao = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Conflito"), null);
        var versaoAntiga = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Conflito"), criada.ETag);
        var inexistente = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{Guid.NewGuid()}", Corpo("Conflito"), criada.ETag);

        semVersao.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        (await semVersao.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.versao_obrigatoria");
        versaoAntiga.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await versaoAntiga.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.conflito_de_versao");
        inexistente.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var atual = (await gestor.GetAsync($"{_rota}/{criada.Id}", TestContext.Current.CancellationToken)).Headers.ETag!.Tag;
        (await gestor.PatchAsJsonAsync($"{_rota}/{criada.Id}/status", new { status = "Concluida" }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var naoEditavel = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Conflito"), atual);
        naoEditavel.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var novoEtag = (await gestor.GetAsync($"{_rota}/{criada.Id}", TestContext.Current.CancellationToken)).Headers.ETag!.Tag;
        var editarConcluida = await Enviar(gestor, HttpMethod.Put, $"{_rota}/{criada.Id}", Corpo("Conflito"), novoEtag);
        editarConcluida.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await editarConcluida.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.nao_editavel");
    }

    /// <summary>
    /// Dado um PATCH de status, quando concluído e repetido, desconhecido ou inexistente, então recebe 204, 422, 400 e 404; o status final vem no evento.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Status_ConcluirCancelarEInvalidos_DevolveCodigosCertos()
    {
        using var gestor = Gestor();
        var criada = await CriarAsync(gestor, "Status");

        var concluir = await gestor.PatchAsJsonAsync($"{_rota}/{criada.Id}/status", new { status = "Concluida" }, TestContext.Current.CancellationToken);
        var cancelar = await gestor.PatchAsJsonAsync($"{_rota}/{criada.Id}/status", new { status = "Cancelada" }, TestContext.Current.CancellationToken);
        var desconhecido = await gestor.PatchAsJsonAsync($"{_rota}/{criada.Id}/status", new { status = "Pausada" }, TestContext.Current.CancellationToken);
        var inexistente = await gestor.PatchAsJsonAsync($"{_rota}/{Guid.NewGuid()}/status", new { status = "Concluida" }, TestContext.Current.CancellationToken);

        concluir.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        cancelar.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await cancelar.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString().ShouldBe("campanha.transicao_invalida");
        desconhecido.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        inexistente.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var outbox = await api.LerOutboxAsync(criada.Id);
        JsonDocument.Parse(outbox[^1]).RootElement.GetProperty("message").GetProperty("status").GetString().ShouldBe("Concluida");
    }

    /// <summary>
    /// Cria um cliente HTTP autenticado como gestor.
    /// </summary>
    /// <returns>O cliente.</returns>
    private HttpClient Gestor()
    {
        return Cliente("GestorONG");
    }

    /// <summary>
    /// Cria um cliente HTTP autenticado com o papel informado.
    /// </summary>
    /// <param name="role">Papel do token.</param>
    /// <returns>O cliente.</returns>
    private HttpClient Cliente(string role)
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.GerarToken(role));
        return cliente;
    }

    /// <summary>
    /// Monta um corpo de campanha válido, trocando só o que o teste precisa.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="meta">Meta da campanha.</param>
    /// <returns>O corpo da requisição.</returns>
    private static object Corpo(string titulo, int meta = 10000)
    {
        return new { titulo, descricao = "Presentes e ceia.", dataInicio = _hoje, dataFim = _hoje.AddDays(30), metaFinanceira = meta };
    }

    /// <summary>
    /// Cria uma campanha e devolve o identificador e o ETag.
    /// </summary>
    /// <param name="cliente">Cliente autenticado como gestor.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>O identificador e o ETag da campanha criada.</returns>
    private static async Task<(Guid Id, string ETag)> CriarAsync(HttpClient cliente, string titulo)
    {
        var resposta = await cliente.PostAsJsonAsync(_rota, Corpo(titulo), TestContext.Current.CancellationToken);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
        return (id, resposta.Headers.ETag!.Tag);
    }

    /// <summary>
    /// Envia uma requisição com corpo JSON e, opcionalmente, o cabeçalho If-Match.
    /// </summary>
    /// <param name="cliente">Cliente HTTP.</param>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="url">Rota.</param>
    /// <param name="corpo">Corpo da requisição.</param>
    /// <param name="ifMatch">Valor do If-Match, ou nulo para não enviar.</param>
    /// <returns>A resposta HTTP.</returns>
    private static async Task<HttpResponseMessage> Enviar(HttpClient cliente, HttpMethod metodo, string url, object corpo, string? ifMatch)
    {
        using var requisicao = new HttpRequestMessage(metodo, url) { Content = JsonContent.Create(corpo) };
        if (ifMatch is not null)
        {
            requisicao.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
    }
}
