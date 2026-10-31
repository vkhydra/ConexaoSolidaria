using ConexaoSolidaria.ServiceDefaults.Telemetria;
using ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Telemetria;

/// <summary>
/// Testes dos traces (OpenTelemetry) e das métricas (Prometheus) padrão dos serviços.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TelemetriaTests
{
    /// <summary>
    /// Dado o endpoint <c>/metrics</c>, quando uma requisição de negócio acontece, então o Prometheus a vê.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Metricas_AposRequisicao_ExpoeContadorHttpEMetricasDoProcesso()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = AplicacaoDeTeste.CriarConstrutor();
        await using var app = builder.Build();
        app.MapearMetricas();
        app.MapGet("/ping", () => "pong");
        await app.StartAsync(ct);
        using var cliente = app.GetTestClient();

        (await cliente.GetAsync("/ping", ct)).EnsureSuccessStatusCode();
        var resposta = await cliente.GetAsync(ExtensoesTelemetria.RotaMetricas, ct);
        var corpo = await resposta.Content.ReadAsStringAsync(ct);

        resposta.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        corpo.ShouldContain("http_requests_received_total");
        corpo.ShouldContain("process_cpu_seconds_total");
    }

    /// <summary>
    /// Dado o tracing ligado, quando uma requisição de negócio acontece, então um span é registrado.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Traces_RequisicaoDeNegocio_GeraSpan()
    {
        var ct = TestContext.Current.CancellationToken;
        var spans = new SpansCollection();
        var builder = AplicacaoDeTeste.CriarConstrutor();
        builder.AddTelemetria("servico-de-teste");
        builder.Services.ConfigureOpenTelemetryTracerProvider(traces => traces.AddInMemoryExporter(spans));
        await using var app = builder.Build();
        app.MapGet("/negocio", () => "ok");
        await app.StartAsync(ct);
        using var cliente = app.GetTestClient();

        (await cliente.GetAsync("/negocio", ct)).EnsureSuccessStatusCode();
        var apareceu = await Aguardar.AteAsync(() => spans.TemCaminho("/negocio"), TimeSpan.FromSeconds(5), ct);

        apareceu.ShouldBeTrue("o span da requisição deveria ter sido exportado");
        spans.Copia().ShouldContain(s => s.OperationName.Contains("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    /// <summary>
    /// Dadas as sondas de saúde e de métricas, quando são chamadas, então não geram span (ao contrário de uma rota de negócio).
    /// </summary>
    /// <param name="caminho">Caminho de sonda ou de métricas chamado.</param>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/metrics")]
    public async Task Traces_SondasEMetricas_NaoGeramSpan(string caminho)
    {
        var ct = TestContext.Current.CancellationToken;
        var spans = new SpansCollection();
        var builder = AplicacaoDeTeste.CriarConstrutor();
        builder.AddTelemetria("servico-de-teste");
        builder.Services.ConfigureOpenTelemetryTracerProvider(traces => traces.AddInMemoryExporter(spans));
        await using var app = builder.Build();
        app.MapGet("/health/live", () => "ok");
        app.MapGet("/negocio", () => "ok");
        app.MapearMetricas();
        await app.StartAsync(ct);
        using var cliente = app.GetTestClient();

        (await cliente.GetAsync(caminho, ct)).EnsureSuccessStatusCode();
        (await cliente.GetAsync("/negocio", ct)).EnsureSuccessStatusCode();
        var controle = await Aguardar.AteAsync(() => spans.TemCaminho("/negocio"), TimeSpan.FromSeconds(5), ct);

        // O ActivitySource do ASP.NET é global: spans de outros testes em paralelo também chegam aqui.
        // Por isso confere-se só o que importa: a rota de controle aparece e a sonda não.
        controle.ShouldBeTrue("a requisição de controle deveria gerar span");
        spans.TemCaminho(caminho).ShouldBeFalse($"{caminho} não deveria gerar span");
    }

    /// <summary>
    /// Dado o serviço informado, quando o tracing é ligado, então o recurso leva nome, versão e ambiente.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task Traces_RecursoDoProvedor_LevaServicoEAmbiente()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = AplicacaoDeTeste.CriarConstrutor("Staging");
        builder.AddTelemetria("campanhas-api");
        await using var app = builder.Build();
        await app.StartAsync(ct);

        var atributos = app.Services.GetRequiredService<TracerProvider>().GetResource().Attributes.ToDictionary(a => a.Key, a => a.Value);

        atributos["service.name"].ShouldBe("campanhas-api");
        atributos["deployment.environment"].ShouldBe("Staging");
        atributos.ShouldContainKey("service.version");
    }

    /// <summary>
    /// Dado o endpoint do coletor configurado (ou não), quando a configuração é lida, então o envio só liga com valor.
    /// </summary>
    /// <param name="endpoint">Valor de <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.</param>
    /// <param name="esperado">Se o exportador deve ser considerado configurado.</param>
    [Theory]
    [InlineData("http://jaeger:4317", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void OtlpConfigurado_DependeDaVariavelDeAmbiente(string? endpoint, bool esperado)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ExtensoesTelemetria.VariavelEndpointOtlp] = endpoint })
            .Build();

        ExtensoesTelemetria.OtlpConfigurado(configuracao).ShouldBe(esperado);
    }

    /// <summary>
    /// Dado o coletor configurado, quando a aplicação é construída, então o exportador OTLP é aceito sem erro.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task AddTelemetria_ComEndpointOtlp_ConstroiAplicacao()
    {
        var builder = AplicacaoDeTeste.CriarConstrutor();
        builder.Configuration[ExtensoesTelemetria.VariavelEndpointOtlp] = "http://localhost:4317";
        builder.AddTelemetria("identity-api", "MassTransit");

        await using var app = builder.Build();

        app.Services.GetRequiredService<TracerProvider>().ShouldNotBeNull();
    }

    /// <summary>
    /// Dados caminhos de sonda, de métricas e de negócio, quando classificados, então só os dois primeiros ficam sem trace.
    /// </summary>
    /// <param name="caminho">Caminho da requisição.</param>
    /// <param name="esperado">Se o caminho deve ficar sem trace.</param>
    [Theory]
    [InlineData("/health", true)]
    [InlineData("/health/ready", true)]
    [InlineData("/METRICS", true)]
    [InlineData("/api/v1/doacoes", false)]
    [InlineData("/healthcare", false)]
    public void CaminhoSemTrace_ClassificaSondasEMetricas(string caminho, bool esperado)
    {
        ExtensoesTelemetria.CaminhoSemTrace(new PathString(caminho)).ShouldBe(esperado);
    }

    /// <summary>
    /// Dados argumentos inválidos, quando o tracing é ligado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void AddTelemetria_ArgumentosInvalidos_LancaExcecao()
    {
        var builder = AplicacaoDeTeste.CriarConstrutor();

        Should.Throw<ArgumentException>(() => builder.AddTelemetria(""));
        Should.Throw<ArgumentNullException>(() => ExtensoesTelemetria.AddTelemetria(null!, "servico"));
        Should.Throw<ArgumentNullException>(() => ExtensoesTelemetria.MapearMetricas(null!));
        Should.Throw<ArgumentNullException>(() => ExtensoesTelemetria.OtlpConfigurado(null!));
    }
}
