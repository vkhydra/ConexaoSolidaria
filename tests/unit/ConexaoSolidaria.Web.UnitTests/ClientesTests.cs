using System.Net;
using ConexaoSolidaria.Web.Servicos;
using ConexaoSolidaria.Web.UnitTests.Suporte;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes dos clientes das APIs: rotas, JWT anexado só quando pedido, cabeçalhos e tradução dos erros.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ClientesTests
{
    /// <summary>
    /// Sessão falsa com um JWT.
    /// </summary>
    private readonly ISessaoDoUsuario _sessao = Substitute.For<ISessaoDoUsuario>();

    /// <summary>
    /// Prepara a sessão com o token <c>jwt-de-teste</c>.
    /// </summary>
    public ClientesTests()
    {
        _sessao.ObterTokenAsync().Returns("jwt-de-teste");
    }

    /// <summary>
    /// Dado um login válido, quando enviado, então vai para a rota de login sem JWT e devolve o token e o papel.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Login_Sucesso_DevolveTokenSemEnviarJwt()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, """{"accessToken":"a.b.c","tokenType":"Bearer","expiresIn":3600,"role":"Doador"}"""));

        var resultado = await CriarIdentity(manipulador).LoginAsync("maria@exemplo.com", "Senha@123", TestContext.Current.CancellationToken);

        resultado.Sucesso.ShouldBeTrue();
        resultado.Valor.ShouldBe(new LoginResposta("a.b.c", "Bearer", 3600, "Doador"));
        var requisicao = manipulador.Requisicoes.ShouldHaveSingleItem();
        requisicao.Uri.ShouldBe("http://identity.teste/api/v1/auth/login");
        requisicao.Autorizacao.ShouldBeNull();
        requisicao.Corpo.ShouldBe("""{"email":"maria@exemplo.com","senha":"Senha@123"}""");
    }

    /// <summary>
    /// Dado um erro 401 em problem+json, quando o login falha, então o erro traz status, código, título e rastreio.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Login_Erro401_TraduzProblemJson()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.Unauthorized, """{"title":"E-mail ou senha inválidos","status":401,"code":"auth.credenciais_invalidas","traceId":"abc"}"""));

        var resultado = await CriarIdentity(manipulador).LoginAsync("x@x.com", "errada", TestContext.Current.CancellationToken);

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe(new ErroApi(401, "auth.credenciais_invalidas", "E-mail ou senha inválidos", new Dictionary<string, string[]>(), "abc"), new ErroApiComparador());
        Should.Throw<InvalidOperationException>(() => resultado.Valor);
    }

    /// <summary>
    /// Dado um erro 400 com campos, quando o cadastro falha, então o erro traz as mensagens por campo.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Cadastrar_Erro400_TrazMensagensPorCampo()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.BadRequest, """{"title":"Dados inválidos","code":"validacao","errors":{"cpf":["CPF inválido."]}}"""));

        var resultado = await CriarIdentity(manipulador).CadastrarAsync(new CadastroDoadorRequest("Maria Silva", "m@x.com", "1", "s", true), TestContext.Current.CancellationToken);

        resultado.Erro!.Campos["cpf"].ShouldBe(["CPF inválido."]);
        manipulador.Requisicoes.Single().Corpo!.ShouldContain("\"aceitePoliticaPrivacidade\":true");
    }

    /// <summary>
    /// Dado um corpo que não é problem+json, quando a API responde erro, então a mensagem vem do status.
    /// </summary>
    /// <param name="status">Status HTTP.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Sua sessão expirou. Entre de novo.")]
    [InlineData(HttpStatusCode.Forbidden, "Você não tem permissão para esta ação.")]
    [InlineData(HttpStatusCode.TooManyRequests, "Muitas tentativas. Tente novamente em alguns minutos.")]
    [InlineData(HttpStatusCode.InternalServerError, "Não foi possível concluir a operação.")]
    public async Task Erro_CorpoQueNaoEJson_UsaMensagemDoStatus(HttpStatusCode status, string mensagem)
    {
        var manipulador = new ManipuladorFalso(_ => new HttpResponseMessage(status) { Content = new StringContent("<html>erro</html>") });

        var resultado = await CriarIdentity(manipulador).ObterMeusDadosAsync(TestContext.Current.CancellationToken);

        resultado.Erro!.Status.ShouldBe((int)status);
        resultado.Erro.Mensagem.ShouldBe(mensagem);
        resultado.Erro.Codigo.ShouldBeNull();
    }

    /// <summary>
    /// Dado um problem+json sem título, quando a API responde erro, então a mensagem vem do status.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Erro_ProblemSemTitulo_UsaMensagemDoStatus()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.Forbidden, """{"code":"x"}"""));

        var resultado = await CriarIdentity(manipulador).ObterMeusDadosAsync(TestContext.Current.CancellationToken);

        resultado.Erro!.Mensagem.ShouldBe("Você não tem permissão para esta ação.");
        resultado.Erro.Codigo.ShouldBe("x");
    }

    /// <summary>
    /// Dado um servidor inalcançável, quando uma chamada é feita, então o erro é 503 <c>servico.indisponivel</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Chamada_ServidorInalcancavel_Devolve503()
    {
        var manipulador = new ManipuladorFalso(_ => throw new HttpRequestException("sem rede"));

        var resultado = await CriarIdentity(manipulador).LoginAsync("a@a.com", "x", TestContext.Current.CancellationToken);

        resultado.Erro!.Status.ShouldBe(503);
        resultado.Erro.Codigo.ShouldBe("servico.indisponivel");
    }

    /// <summary>
    /// Dado um cancelamento pedido, quando a chamada é feita, então a exceção de cancelamento sobe (não vira erro 503).
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Chamada_Cancelada_PropagaCancelamento()
    {
        using var origem = new CancellationTokenSource();
        await origem.CancelAsync();
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, "{}"));

        await Should.ThrowAsync<OperationCanceledException>(() => CriarIdentity(manipulador).LoginAsync("a@a.com", "x", origem.Token));
    }

    /// <summary>
    /// Dado o titular logado, quando consulta e exclui os dados, então as chamadas levam o JWT e usam GET e DELETE em <c>/doadores/me</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task MeusDados_ConsultarEExcluir_LevamOJwt()
    {
        var manipulador = new ManipuladorFalso(r => r.Method == HttpMethod.Get
            ? ManipuladorFalso.Json(HttpStatusCode.OK, """{"id":"0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70","nomeCompleto":"Maria Silva","email":"m@x.com","cpfMascarado":"***.982.247-**","criadoEm":"2026-11-10T14:03:22-03:00","consentimento":{"versao":"2026-10","aceitoEm":"2026-11-10T14:03:22-03:00"}}""")
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var cliente = CriarIdentity(manipulador);

        var dados = await cliente.ObterMeusDadosAsync(TestContext.Current.CancellationToken);
        var exclusao = await cliente.ExcluirMeusDadosAsync(TestContext.Current.CancellationToken);

        dados.Valor.CpfMascarado.ShouldBe("***.982.247-**");
        dados.Valor.Consentimento.Versao.ShouldBe("2026-10");
        exclusao.Sucesso.ShouldBeTrue();
        manipulador.Requisicoes.Select(r => (r.Metodo.Method, r.Uri, r.Autorizacao)).ShouldBe(
        [
            ("GET", "http://identity.teste/api/v1/doadores/me", "Bearer jwt-de-teste"),
            ("DELETE", "http://identity.teste/api/v1/doadores/me", "Bearer jwt-de-teste"),
        ]);
    }

    /// <summary>
    /// Dada a ausência de sessão, quando uma chamada autenticada é feita, então ela sai sem cabeçalho Authorization.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task ChamadaAutenticada_SemToken_NaoEnviaAuthorization()
    {
        _sessao.ObterTokenAsync().Returns((string?)null);
        var manipulador = new ManipuladorFalso(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await CriarIdentity(manipulador).ExcluirMeusDadosAsync(TestContext.Current.CancellationToken);

        manipulador.Requisicoes.Single().Autorizacao.ShouldBeNull();
    }

    /// <summary>
    /// Dado o painel público, quando consultado, então a lista é lida sem JWT.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Painel_Consulta_LeListaSemJwt()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, """[{"id":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","titulo":"Natal","metaFinanceira":10000.00,"totalArrecadado":150.00,"percentualAtingido":1.50,"dataFim":"2026-12-24"}]"""));

        var resultado = await CriarCampanhas(manipulador).ObterPainelAsync(TestContext.Current.CancellationToken);

        var item = resultado.Valor.ShouldHaveSingleItem();
        item.Titulo.ShouldBe("Natal");
        item.PercentualAtingido.ShouldBe(1.50m);
        item.DataFim.ShouldBe(new DateOnly(2026, 12, 24));
        manipulador.Requisicoes.Single().Autorizacao.ShouldBeNull();
        manipulador.Requisicoes.Single().Uri.ShouldBe("http://campanhas.teste/api/v1/transparencia/campanhas");
    }

    /// <summary>
    /// Dado um texto com espaços e acentos, quando se busca, então ele vai codificado na consulta, com página e tamanho.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Buscar_TextoComEspacos_VaiCodificado()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, """{"items":[{"id":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","titulo":"Natal Solidário","trecho":"<em>ceia</em>","relevancia":3.4,"dataFim":"2026-12-24"}],"page":2,"pageSize":5,"totalItems":6,"totalPages":2}"""));

        var resultado = await CriarCampanhas(manipulador).BuscarAsync("natl solidário", 2, 5, TestContext.Current.CancellationToken);

        resultado.Valor.Items.Single().Trecho.ShouldBe("<em>ceia</em>");
        resultado.Valor.TotalPages.ShouldBe(2);
        manipulador.Requisicoes.Single().Uri.ShouldBe("http://campanhas.teste/api/v1/campanhas/busca?q=natl%20solid%C3%A1rio&page=2&pageSize=5");
    }

    /// <summary>
    /// Dado um filtro de status, quando se lista a gestão, então a consulta leva o status, a página e o JWT.
    /// </summary>
    /// <param name="status">Filtro de status.</param>
    /// <param name="consulta">Consulta esperada.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData("Ativa", "status=Ativa&page=1&pageSize=20")]
    [InlineData(null, "page=1&pageSize=20")]
    [InlineData("", "page=1&pageSize=20")]
    public async Task Listar_ComEsemFiltro_MontaAConsulta(string? status, string consulta)
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, """{"items":[],"page":1,"pageSize":20,"totalItems":0,"totalPages":0}"""));

        var resultado = await CriarCampanhas(manipulador).ListarAsync(status, cancelamento: TestContext.Current.CancellationToken);

        resultado.Sucesso.ShouldBeTrue();
        manipulador.Requisicoes.Single().Uri.ShouldBe($"http://campanhas.teste/api/v1/campanhas?{consulta}");
        manipulador.Requisicoes.Single().Autorizacao.ShouldBe("Bearer jwt-de-teste");
    }

    /// <summary>
    /// Dada uma campanha, quando obtida, então o ETag da resposta acompanha a campanha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Obter_Campanha_TrazOETag()
    {
        var id = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");
        var manipulador = new ManipuladorFalso(_ =>
        {
            var resposta = ManipuladorFalso.Json(HttpStatusCode.OK, """{"id":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","titulo":"Natal","descricao":"d","dataInicio":"2026-11-10","dataFim":"2026-12-24","metaFinanceira":10000.00,"totalArrecadado":0.00,"percentualAtingido":0.00,"status":"Ativa"}""");
            resposta.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"7\"");
            return resposta;
        });

        var resultado = await CriarCampanhas(manipulador).ObterAsync(id, TestContext.Current.CancellationToken);

        resultado.Valor.Versao.ShouldBe("\"7\"");
        resultado.Valor.Campanha.Status.ShouldBe("Ativa");
        manipulador.Requisicoes.Single().Uri.ShouldBe($"http://campanhas.teste/api/v1/campanhas/{id}");
    }

    /// <summary>
    /// Dada uma resposta sem ETag, quando a campanha é obtida, então a versão fica vazia.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Obter_SemETag_VersaoVazia()
    {
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.OK, """{"id":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","titulo":"N","descricao":"d","dataInicio":"2026-11-10","dataFim":"2026-12-24","metaFinanceira":1.00,"totalArrecadado":0,"percentualAtingido":0,"status":"Ativa"}"""));

        (await CriarCampanhas(manipulador).ObterAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)).Valor.Versao.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Dados criar, editar e mudar o status, quando chamados, então usam POST, PUT com If-Match e PATCH com JWT.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Gestao_CriarEditarEMudarStatus_UsamOsVerbosEOIfMatch()
    {
        var id = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");
        var campanhaJson = """{"id":"0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f","titulo":"N","descricao":"d","dataInicio":"2026-11-10","dataFim":"2026-12-24","metaFinanceira":1.00,"totalArrecadado":0,"percentualAtingido":0,"status":"Ativa"}""";
        var manipulador = new ManipuladorFalso(r => r.Method == HttpMethod.Patch ? new HttpResponseMessage(HttpStatusCode.NoContent) : ManipuladorFalso.Json(HttpStatusCode.OK, campanhaJson));
        var cliente = CriarCampanhas(manipulador);
        var dados = new CampanhaRequest("Nova", "Desc", new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 24), 5000m);

        var criada = await cliente.CriarAsync(dados, TestContext.Current.CancellationToken);
        var editada = await cliente.AtualizarAsync(id, "\"7\"", dados, TestContext.Current.CancellationToken);
        var status = await cliente.AlterarStatusAsync(id, "Cancelada", TestContext.Current.CancellationToken);

        criada.Sucesso.ShouldBeTrue();
        editada.Sucesso.ShouldBeTrue();
        status.Sucesso.ShouldBeTrue();
        manipulador.Requisicoes.Select(r => (r.Metodo.Method, r.IfMatch)).ShouldBe([("POST", null), ("PUT", "\"7\""), ("PATCH", null)]);
        manipulador.Requisicoes[0].Corpo.ShouldBe("""{"titulo":"Nova","descricao":"Desc","dataInicio":"2026-11-10","dataFim":"2026-12-24","metaFinanceira":5000}""");
        manipulador.Requisicoes[2].Corpo.ShouldBe("""{"status":"Cancelada"}""");
        manipulador.Requisicoes.ShouldAllBe(r => r.Autorizacao == "Bearer jwt-de-teste");
    }

    /// <summary>
    /// Dada uma doação, quando enviada, então leva a chave de idempotência e o JWT, e a resposta 202 é lida.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Doar_Aceita_LevaChaveDeIdempotencia()
    {
        var campanha = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");
        var manipulador = new ManipuladorFalso(_ => ManipuladorFalso.Json(HttpStatusCode.Accepted, """{"doacaoId":"0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f","status":"Recebida","mensagem":"Doação recebida."}"""));

        var resultado = await CriarCampanhas(manipulador).DoarAsync(campanha, 150.50m, "chave-1", TestContext.Current.CancellationToken);

        resultado.Valor.Status.ShouldBe("Recebida");
        var requisicao = manipulador.Requisicoes.Single();
        requisicao.ChaveDeIdempotencia.ShouldBe("chave-1");
        requisicao.Autorizacao.ShouldBe("Bearer jwt-de-teste");
        requisicao.Corpo.ShouldBe($$"""{"idCampanha":"{{campanha}}","valorDoacao":150.50}""");
    }

    /// <summary>
    /// Dado o manipulador do JWT, quando a requisição não traz token, então nada é anexado; com token, vira Bearer.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task AnexarJwtHandler_ComESemToken_AnexaSoQuandoHa()
    {
        var interno = new ManipuladorFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cliente = new HttpClient(new AnexarJwtHandler { InnerHandler = interno }) { BaseAddress = new Uri("http://x.teste/") };
        using var comToken = new HttpRequestMessage(HttpMethod.Get, "a");
        comToken.Options.Set(AnexarJwtHandler.ChaveDoToken, "tok");
        using var vazio = new HttpRequestMessage(HttpMethod.Get, "b");
        vazio.Options.Set(AnexarJwtHandler.ChaveDoToken, " ");

        await cliente.SendAsync(comToken, TestContext.Current.CancellationToken);
        await cliente.SendAsync(vazio, TestContext.Current.CancellationToken);
        await cliente.GetAsync("c", TestContext.Current.CancellationToken);

        interno.Requisicoes.Select(r => r.Autorizacao).ShouldBe(["Bearer tok", null, null]);
    }

    /// <summary>
    /// Cria o cliente da Identity sobre o manipulador falso.
    /// </summary>
    /// <param name="manipulador">Manipulador falso.</param>
    /// <returns>O cliente.</returns>
    private ClienteIdentity CriarIdentity(ManipuladorFalso manipulador)
    {
        return new ClienteIdentity(Http(manipulador, "http://identity.teste/"), _sessao);
    }

    /// <summary>
    /// Cria o cliente de Campanhas sobre o manipulador falso.
    /// </summary>
    /// <param name="manipulador">Manipulador falso.</param>
    /// <returns>O cliente.</returns>
    private ClienteCampanhas CriarCampanhas(ManipuladorFalso manipulador)
    {
        return new ClienteCampanhas(Http(manipulador, "http://campanhas.teste/"), _sessao);
    }

    /// <summary>
    /// Cria um <see cref="HttpClient"/> com o manipulador do JWT na frente do falso, como na aplicação.
    /// </summary>
    /// <param name="manipulador">Manipulador falso.</param>
    /// <param name="baseUri">Endereço base.</param>
    /// <returns>O cliente HTTP.</returns>
    private static HttpClient Http(ManipuladorFalso manipulador, string baseUri)
    {
        return new HttpClient(new AnexarJwtHandler { InnerHandler = manipulador }) { BaseAddress = new Uri(baseUri) };
    }

    /// <summary>
    /// Compara dois <see cref="ErroApi"/> pelos campos, já que o dicionário de campos não tem igualdade por valor.
    /// </summary>
    private sealed class ErroApiComparador : IEqualityComparer<ErroApi>
    {
        /// <inheritdoc/>
        public bool Equals(ErroApi? x, ErroApi? y)
        {
            return x is not null && y is not null && x.Status == y.Status && x.Codigo == y.Codigo && x.Mensagem == y.Mensagem && x.TraceId == y.TraceId && x.Campos.Count == y.Campos.Count;
        }

        /// <inheritdoc/>
        public int GetHashCode(ErroApi obj)
        {
            return HashCode.Combine(obj.Status, obj.Codigo);
        }
    }
}
