using System.Net;
using System.Text.Json;
using ConexaoSolidaria.ServiceDefaults.Erros;
using ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Erros;

/// <summary>
/// Testes do tratamento global de erros: formato <c>ProblemDetails</c>, códigos estáveis,
/// <c>traceId</c> e ausência de vazamento de detalhes internos.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ErrosTests
{
    /// <summary>
    /// Dada uma exceção não tratada em produção, quando a rota é chamada, então responde 500 sem vazar a mensagem.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task ExcecaoNaoTratada_EmProducao_Retorna500SemVazarDetalhes()
    {
        var ct = TestContext.Current.CancellationToken;
        var logs = new ProvedorLogEmMemoria();
        await using var app = await IniciarAsync("Production", logs, rotas => rotas.MapGet("/quebra", ExplodirAsync), ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync("/quebra", ct);
        var corpo = await resposta.Content.ReadAsStringAsync(ct);
        var json = JsonDocument.Parse(corpo).RootElement;

        resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        json.GetProperty("code").GetString().ShouldBe(CatalogoDeErros.ErroInesperado);
        json.GetProperty("title").GetString().ShouldBe("Erro inesperado");
        json.GetProperty("type").GetString().ShouldBe(CatalogoDeErros.PrefixoTipo + CatalogoDeErros.ErroInesperado);
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        corpo.ShouldNotContain("senha-do-banco");
        corpo.ShouldNotContain("StackTrace");
        json.TryGetProperty("detail", out _).ShouldBeFalse();
        logs.Entradas.ShouldContain(e => e.Nivel == LogLevel.Error && e.Mensagem.Contains("/quebra", StringComparison.Ordinal));
    }

    /// <summary>
    /// Dada uma exceção em desenvolvimento, quando a rota é chamada, então a mensagem ajuda o desenvolvedor, sem stack trace.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task ExcecaoNaoTratada_EmDesenvolvimento_MostraMensagemSemStackTrace()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync("Development", new ProvedorLogEmMemoria(), rotas => rotas.MapGet("/quebra", ExplodirAsync), ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync("/quebra", ct);
        var corpo = await resposta.Content.ReadAsStringAsync(ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        corpo.ShouldContain("senha-do-banco");
        corpo.ShouldNotContain("StackTrace");
        corpo.ShouldNotContain(" at ConexaoSolidaria");
    }

    /// <summary>
    /// Dada uma requisição mal formada, quando o servidor a rejeita, então a resposta é 400 com código de validação.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task RequisicaoMalFormada_Retorna400ComCodigoDeValidacao()
    {
        var ct = TestContext.Current.CancellationToken;
        var logs = new ProvedorLogEmMemoria();
        await using var app = await IniciarAsync("Production", logs, rotas => rotas.MapGet("/invalida", RejeitarAsync), ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync("/invalida", ct);
        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct)).RootElement;

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        json.GetProperty("code").GetString().ShouldBe(CatalogoDeErros.Validacao);
        json.GetProperty("title").GetString().ShouldBe("Dados inválidos");
        logs.Entradas.ShouldContain(e => e.Nivel == LogLevel.Warning);
        logs.Entradas.ShouldNotContain(e => e.Nivel == LogLevel.Error);
    }

    /// <summary>
    /// Dados status de erro sem corpo, quando a resposta é gerada, então vira ProblemDetails com código e título padrão.
    /// </summary>
    /// <param name="caminho">Rota que devolve o status.</param>
    /// <param name="status">Status HTTP esperado.</param>
    /// <param name="codigo">Código esperado.</param>
    /// <param name="titulo">Título esperado.</param>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Theory]
    [InlineData("/nao-existe", 404, CatalogoDeErros.NaoEncontrado, "Recurso não encontrado")]
    [InlineData("/nao-autenticado", 401, CatalogoDeErros.NaoAutenticado, "Autenticação necessária")]
    [InlineData("/sem-permissao", 403, CatalogoDeErros.SemPermissao, "Acesso negado")]
    [InlineData("/teapot", 418, CatalogoDeErros.ErroHttp, "Falha na requisição")]
    public async Task StatusDeErroSemCorpo_ViraProblemDetailsComCodigoPadrao(string caminho, int status, string codigo, string titulo)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            "Production",
            new ProvedorLogEmMemoria(),
            rotas =>
            {
                rotas.MapGet("/nao-autenticado", () => Results.StatusCode(StatusCodes.Status401Unauthorized));
                rotas.MapGet("/sem-permissao", () => Results.StatusCode(StatusCodes.Status403Forbidden));
                rotas.MapGet("/teapot", () => Results.StatusCode(StatusCodes.Status418ImATeapot));
            },
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(caminho, ct);
        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct)).RootElement;

        ((int)resposta.StatusCode).ShouldBe(status);
        json.GetProperty("status").GetInt32().ShouldBe(status);
        json.GetProperty("code").GetString().ShouldBe(codigo);
        json.GetProperty("title").GetString().ShouldBe(titulo);
        json.GetProperty("instance").GetString().ShouldBe(caminho);
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Dado um erro de regra de negócio com código e título próprios, quando a resposta é completada, então eles são preservados.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task ProblemaDeRegraDeNegocio_PreservaCodigoETituloProprios()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            "Production",
            new ProvedorLogEmMemoria(),
            rotas => rotas.MapGet(
                "/regra",
                () => Results.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Meta inválida",
                    extensions: new Dictionary<string, object?> { ["code"] = "campanha.meta_invalida" })),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync("/regra", ct);
        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct)).RootElement;

        ((int)resposta.StatusCode).ShouldBe(422);
        json.GetProperty("code").GetString().ShouldBe("campanha.meta_invalida");
        json.GetProperty("title").GetString().ShouldBe("Meta inválida");
        json.GetProperty("type").GetString().ShouldBe(CatalogoDeErros.PrefixoTipo + "campanha.meta_invalida");
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Dados status conhecidos e desconhecidos, quando o catálogo é consultado, então códigos e títulos são estáveis.
    /// </summary>
    /// <param name="status">Status HTTP.</param>
    /// <param name="codigo">Código esperado.</param>
    [Theory]
    [InlineData(400, CatalogoDeErros.Validacao)]
    [InlineData(401, CatalogoDeErros.NaoAutenticado)]
    [InlineData(403, CatalogoDeErros.SemPermissao)]
    [InlineData(404, CatalogoDeErros.NaoEncontrado)]
    [InlineData(500, CatalogoDeErros.ErroInesperado)]
    [InlineData(409, CatalogoDeErros.ErroHttp)]
    public void Catalogo_CodigoPara_RetornaCodigoEstavel(int status, string codigo)
    {
        CatalogoDeErros.CodigoPara(status).ShouldBe(codigo);
        CatalogoDeErros.TituloPara(status).ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Dados argumentos nulos, quando o tratamento de erros é configurado ou usado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Erros_ArgumentosNulos_LancaExcecao()
    {
        var ct = TestContext.Current.CancellationToken;
        var tratador = new TratadorExcecoes(null!, null!, null!);

        Should.Throw<ArgumentNullException>(() => ExtensoesErros.AddTratamentoErros(null!));
        Should.Throw<ArgumentNullException>(() => ExtensoesErros.UsarTratamentoErros(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await tratador.TryHandleAsync(null!, new InvalidOperationException(), ct));
        await Should.ThrowAsync<ArgumentNullException>(async () => await tratador.TryHandleAsync(new DefaultHttpContext(), null!, ct));
    }

    /// <summary>
    /// Rota de teste que lança uma exceção com um segredo na mensagem.
    /// </summary>
    /// <returns>Nunca retorna normalmente.</returns>
    private static Task<string> ExplodirAsync()
    {
        throw new InvalidOperationException("falha ao conectar com Password=senha-do-banco");
    }

    /// <summary>
    /// Rota de teste que simula uma requisição HTTP mal formada.
    /// </summary>
    /// <returns>Nunca retorna normalmente.</returns>
    private static Task<string> RejeitarAsync()
    {
        throw new BadHttpRequestException("corpo da requisição inválido");
    }

    /// <summary>
    /// Inicia uma aplicação de teste com o tratamento de erros ligado.
    /// </summary>
    /// <param name="ambiente">Nome do ambiente.</param>
    /// <param name="logs">Provedor que captura os logs.</param>
    /// <param name="mapear">Ação que registra as rotas do teste.</param>
    /// <param name="ct">Token de cancelamento do teste.</param>
    /// <returns>Aplicação iniciada.</returns>
    private static async Task<WebApplication> IniciarAsync(string ambiente, ProvedorLogEmMemoria logs, Action<WebApplication> mapear, CancellationToken ct)
    {
        var builder = AplicacaoDeTeste.CriarConstrutor(ambiente);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.AddTratamentoErros();
        var app = builder.Build();
        app.UsarTratamentoErros();
        mapear(app);
        await app.StartAsync(ct);
        return app;
    }
}
