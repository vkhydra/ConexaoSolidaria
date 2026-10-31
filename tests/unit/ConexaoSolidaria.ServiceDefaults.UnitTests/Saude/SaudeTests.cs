using System.Net;
using System.Text.Json;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Saude;

/// <summary>
/// Testes das sondas de vida e de prontidão e do formato da resposta.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SaudeTests
{
    /// <summary>
    /// Dada uma dependência fora do ar, quando a sonda de vida é chamada, então continua respondendo 200.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Vida_ComDependenciaForaDoAr_ContinuaSaudavel()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck("banco", () => HealthCheckResult.Unhealthy("fora do ar"), [ExtensoesSaude.EtiquetaProntidao]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaVida, ct);
        var json = await LerAsync(resposta, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    /// <summary>
    /// Dado um serviço sem dependências registradas, quando a sonda de prontidão é chamada, então está pronto.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_SemVerificacoes_RetornaSaudavel()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(_ => { }, ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dada uma dependência saudável, quando a sonda de prontidão é chamada, então o nome dela aparece no JSON.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_DependenciaSaudavel_RetornaOkComNomeDaVerificacao()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck("postgres", () => HealthCheckResult.Healthy("conectado"), [ExtensoesSaude.EtiquetaProntidao]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);
        var json = await LerAsync(resposta, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        json.GetProperty("status").GetString().ShouldBe("Healthy");
        json.GetProperty("verificacoes").GetProperty("postgres").GetProperty("descricao").GetString().ShouldBe("conectado");
        json.GetProperty("duracaoMs").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Dada uma dependência fora do ar, quando a sonda de prontidão é chamada, então responde 503.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_DependenciaForaDoAr_Retorna503()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck("rabbitmq", () => HealthCheckResult.Unhealthy("sem conexão"), [ExtensoesSaude.EtiquetaProntidao]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);
        var json = await LerAsync(resposta, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        json.GetProperty("status").GetString().ShouldBe("Unhealthy");
        json.GetProperty("verificacoes").GetProperty("rabbitmq").GetProperty("status").GetString().ShouldBe("Unhealthy");
    }

    /// <summary>
    /// Dada uma dependência degradada (como o Elasticsearch), quando a prontidão é chamada, então o serviço segue pronto.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_DependenciaDegradada_RetornaOkComEstadoDegradado()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck("elasticsearch", () => HealthCheckResult.Degraded("lento"), [ExtensoesSaude.EtiquetaProntidao]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);
        var json = await LerAsync(resposta, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("status").GetString().ShouldBe("Degraded");
    }

    /// <summary>
    /// Dada uma verificação sem a etiqueta de prontidão, quando a prontidão é chamada, então ela não é executada.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_VerificacaoSemEtiqueta_NaoEhExecutada()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck("opcional", () => HealthCheckResult.Unhealthy("ignorada"), ["outra"]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);
        var json = await LerAsync(resposta, ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("verificacoes").EnumerateObject().ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma verificação que lança exceção com um segredo, quando a prontidão é chamada, então o segredo não vaza.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Prontidao_VerificacaoQueLancaExcecao_NaoVazaDetalhes()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await IniciarAsync(
            saude => saude.AddCheck(
                "postgres",
                () => throw new InvalidOperationException("Host=db;Password=segredo-123"),
                [ExtensoesSaude.EtiquetaProntidao]),
            ct);
        using var cliente = app.GetTestClient();

        var resposta = await cliente.GetAsync(ExtensoesSaude.RotaProntidao, ct);
        var corpo = await resposta.Content.ReadAsStringAsync(ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        corpo.ShouldNotContain("segredo-123");
        corpo.ShouldNotContain("Password");
    }

    /// <summary>
    /// Dados argumentos nulos, quando os recursos de saúde são usados, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Saude_ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => ExtensoesSaude.AddSaude(null!));
        Should.Throw<ArgumentNullException>(() => ExtensoesSaude.MapearSaude(null!));
        Should.Throw<ArgumentNullException>(() => EscritorRespostaSaude.EscreverAsync(null!, new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero)));
        Should.Throw<ArgumentNullException>(() => EscritorRespostaSaude.EscreverAsync(new Microsoft.AspNetCore.Http.DefaultHttpContext(), null!));
    }

    /// <summary>
    /// Inicia uma aplicação de teste com as rotas de saúde e as dependências informadas.
    /// </summary>
    /// <param name="configurar">Ação que registra as verificações de dependência.</param>
    /// <param name="ct">Token de cancelamento do teste.</param>
    /// <returns>Aplicação iniciada.</returns>
    private static async Task<WebApplication> IniciarAsync(Action<IHealthChecksBuilder> configurar, CancellationToken ct)
    {
        var builder = AplicacaoDeTeste.CriarConstrutor();
        configurar(builder.AddSaude());
        var app = builder.Build();
        app.MapearSaude();
        await app.StartAsync(ct);
        return app;
    }

    /// <summary>
    /// Lê o corpo da resposta como JSON.
    /// </summary>
    /// <param name="resposta">Resposta HTTP.</param>
    /// <param name="ct">Token de cancelamento do teste.</param>
    /// <returns>Elemento raiz do JSON.</returns>
    private static async Task<JsonElement> LerAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct)).RootElement.Clone();
    }
}
