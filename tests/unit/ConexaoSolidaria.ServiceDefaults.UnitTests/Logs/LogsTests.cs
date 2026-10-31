using System.Diagnostics;
using System.Text.Json;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Logs;

/// <summary>
/// Testes dos logs estruturados: enriquecimento com trace, propriedades fixas do serviço,
/// formato JSON e nível de log das requisições.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LogsTests
{
    /// <summary>
    /// Dada uma atividade em andamento, quando o log é emitido, então traceId e spanId acompanham a linha.
    /// </summary>
    [Fact]
    public void Enriquecedor_ComAtividadeAtual_AdicionaTraceIdESpanId()
    {
        using var ouvinte = CriarOuvinte();
        using var fonte = new ActivitySource("testes.logs");
        using var atividade = fonte.StartActivity("operacao");
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().Enrich.With<EnriquecedorTrace>().WriteTo.Sink(sink).CreateLogger();

        logger.Information("mensagem");

        var evento = sink.Eventos.ShouldHaveSingleItem();
        evento.Properties[EnriquecedorTrace.PropriedadeTraceId].ToString().ShouldContain(atividade!.TraceId.ToString());
        evento.Properties[EnriquecedorTrace.PropriedadeSpanId].ToString().ShouldContain(atividade.SpanId.ToString());
    }

    /// <summary>
    /// Dado que não há atividade, quando o log é emitido, então nenhuma propriedade de trace é inventada.
    /// </summary>
    [Fact]
    public void Enriquecedor_SemAtividadeAtual_NaoAdicionaPropriedades()
    {
        Activity.Current = null;
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().Enrich.With<EnriquecedorTrace>().WriteTo.Sink(sink).CreateLogger();

        logger.Information("mensagem");

        var evento = sink.Eventos.ShouldHaveSingleItem();
        evento.Properties.ShouldNotContainKey(EnriquecedorTrace.PropriedadeTraceId);
        evento.Properties.ShouldNotContainKey(EnriquecedorTrace.PropriedadeSpanId);
    }

    /// <summary>
    /// Dado o padrão do projeto, quando um log é emitido, então serviço, versão e ambiente vão na linha.
    /// </summary>
    [Fact]
    public void ConfigurarPadrao_AdicionaServicoVersaoEAmbiente()
    {
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().ConfigurarPadrao("campanhas-api", "1.2.3", "Production").WriteTo.Sink(sink).CreateLogger();

        logger.Information("mensagem");

        var propriedades = sink.Eventos.ShouldHaveSingleItem().Properties;
        propriedades["service"].ToString().ShouldBe("\"campanhas-api\"");
        propriedades["version"].ToString().ShouldBe("\"1.2.3\"");
        propriedades["environment"].ToString().ShouldBe("\"Production\"");
    }

    /// <summary>
    /// Dado o formatador JSON compacto, quando o log é escrito, então a linha é um JSON válido com a mensagem renderizada.
    /// </summary>
    [Fact]
    public void ConfigurarPadrao_FormataLinhaComoJsonComMensagemRenderizada()
    {
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().ConfigurarPadrao("identity-api", "1.0.0", "Development").WriteTo.Sink(sink).CreateLogger();
        var doacaoId = Guid.NewGuid();

        logger.Information("Doação {DoacaoId} processada", doacaoId);

        using var escritor = new StringWriter();
        new RenderedCompactJsonFormatter().Format(sink.Eventos.ShouldHaveSingleItem(), escritor);
        using var json = JsonDocument.Parse(escritor.ToString());
        json.RootElement.GetProperty("@m").GetString().ShouldBe($"Doação {doacaoId} processada");
        json.RootElement.GetProperty("service").GetString().ShouldBe("identity-api");
    }

    /// <summary>
    /// Dados logs de categorias ruidosas do ASP.NET, quando são de nível Information, então são descartados.
    /// </summary>
    [Fact]
    public void ConfigurarPadrao_CategoriaDoAspNetAbaixoDeWarning_EhDescartada()
    {
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().ConfigurarPadrao("web", "1.0.0", "Development").WriteTo.Sink(sink).CreateLogger();
        var categoria = logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting.Diagnostics");

        categoria.Information("ruído");
        categoria.Warning("anomalia");

        sink.Eventos.ShouldHaveSingleItem().Level.ShouldBe(LogEventLevel.Warning);
    }

    /// <summary>
    /// Dadas requisições de negócio, de sonda e com falha, quando o nível é calculado, então segue a regra do projeto.
    /// </summary>
    /// <param name="caminho">Caminho da requisição.</param>
    /// <param name="status">Status HTTP da resposta.</param>
    /// <param name="comExcecao">Se houve exceção não tratada.</param>
    /// <param name="esperado">Nível esperado.</param>
    [Theory]
    [InlineData("/api/v1/transparencia/campanhas", 200, false, LogEventLevel.Information)]
    [InlineData("/api/v1/doacoes", 202, false, LogEventLevel.Information)]
    [InlineData("/api/v1/doacoes", 422, false, LogEventLevel.Information)]
    [InlineData("/api/v1/doacoes", 500, false, LogEventLevel.Error)]
    [InlineData("/api/v1/doacoes", 200, true, LogEventLevel.Error)]
    [InlineData("/health/live", 200, false, LogEventLevel.Verbose)]
    [InlineData("/HEALTH/ready", 503, false, LogEventLevel.Verbose)]
    [InlineData("/metrics", 200, false, LogEventLevel.Verbose)]
    [InlineData("/health/ready", 200, true, LogEventLevel.Error)]
    [InlineData("/healthcare", 200, false, LogEventLevel.Information)]
    public void NivelRequisicao_Obter_SegueARegraDoProjeto(string caminho, int status, bool comExcecao, LogEventLevel esperado)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = caminho;
        contexto.Response.StatusCode = status;

        var nivel = NivelRequisicao.Obter(contexto, 1.5, comExcecao ? new InvalidOperationException("falha") : null);

        nivel.ShouldBe(esperado);
    }

    /// <summary>
    /// Dado um contexto nulo, quando o nível é calculado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void NivelRequisicao_Obter_ContextoNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => NivelRequisicao.Obter(null!, 1, null));
    }

    /// <summary>
    /// Dados valores configurados e vazios, quando a versão é escolhida, então o configurado vence e o vazio cai no assembly.
    /// </summary>
    /// <param name="configurado">Valor de <c>APP_VERSION</c>.</param>
    /// <param name="esperadoVazio">Se o resultado deve vir do assembly (e não do valor configurado).</param>
    [Theory]
    [InlineData("sha-abc1234", false)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public void ObterVersao_EscolheConfiguradoOuAssembly(string? configurado, bool esperadoVazio)
    {
        var versao = ExtensoesLogs.ObterVersao(configurado);

        versao.ShouldNotBeNullOrWhiteSpace();
        if (esperadoVazio)
        {
            versao.ShouldNotBe(configurado ?? string.Empty);
        }
        else
        {
            versao.ShouldBe(configurado);
        }
    }

    /// <summary>
    /// Dados um assembly ausente e um assembly real, quando a versão é lida, então ausente vale 0.0.0 e o real tem versão.
    /// </summary>
    [Fact]
    public void VersaoDe_AssemblyAusenteOuReal_RetornaZeroOuVersao()
    {
        ExtensoesLogs.VersaoDe(null).ShouldBe("0.0.0");
        ExtensoesLogs.VersaoDe(typeof(LogsTests).Assembly).ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Dados argumentos nulos ou vazios, quando o logger é configurado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void AddLogsEstruturados_ServicoVazio_LancaExcecao()
    {
        var builder = WebApplication.CreateBuilder();

        Should.Throw<ArgumentException>(() => builder.AddLogsEstruturados(" "));
        Should.Throw<ArgumentNullException>(() => ExtensoesLogs.AddLogsEstruturados(null!, "servico"));
    }

    /// <summary>
    /// Dado o serviço informado, quando os logs estruturados são ligados, então o Serilog fica registrado no host.
    /// </summary>
    [Fact]
    public void AddLogsEstruturados_ComServico_RegistraSerilogNoHost()
    {
        var builder = WebApplication.CreateBuilder();

        var retorno = builder.AddLogsEstruturados("web-de-teste");
        using var app = builder.Build();

        retorno.ShouldBeSameAs(builder);
        app.Services.GetService(typeof(Serilog.IDiagnosticContext)).ShouldNotBeNull();
        app.Services.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory)).ShouldNotBeNull();
    }

    /// <summary>
    /// Dado um sink registrado no contêiner de serviços, quando um log é emitido, então o sink recebe o evento.
    /// </summary>
    [Fact]
    public void AddLogsEstruturados_SinkNoContainer_RecebeOsEventos()
    {
        var sink = new SinkEmMemoria();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<Serilog.Core.ILogEventSink>(sink);
        builder.AddLogsEstruturados("web-de-teste");
        using var app = builder.Build();

        app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LogsTests>>().LogInformation("mensagem de teste");

        sink.Eventos.ShouldContain(e => e.RenderMessage() == "mensagem de teste");
    }

    /// <summary>
    /// Dadas uma rota de negócio e uma sonda, quando o log de requisições está ligado, então só a rota de negócio é logada.
    /// </summary>
    /// <returns>Tarefa assíncrona do teste.</returns>
    [Fact]
    public async Task UsarLogsDeRequisicoes_LogaRotaDeNegocioENaoLogaSonda()
    {
        var ct = TestContext.Current.CancellationToken;
        var sink = new SinkEmMemoria();
        using var logger = new LoggerConfiguration().ConfigurarPadrao("api-de-teste", "1.0.0", "Test").WriteTo.Sink(sink).CreateLogger();
        var builder = AplicacaoDeTeste.CriarConstrutor();
        builder.Host.UseSerilog(logger);
        await using var app = builder.Build();
        app.UsarLogsDeRequisicoes(logger);
        app.MapGet("/negocio", () => "ok");
        app.MapGet("/health/live", () => "ok");
        await app.StartAsync(ct);
        using var cliente = app.GetTestClient();

        (await cliente.GetAsync("/health/live", ct)).EnsureSuccessStatusCode();
        (await cliente.GetAsync("/negocio", ct)).EnsureSuccessStatusCode();

        var caminhos = sink.Eventos.Select(e => e.Properties.TryGetValue("RequestPath", out var valor) ? valor.ToString().Trim('"') : string.Empty).ToList();
        caminhos.ShouldContain("/negocio");
        caminhos.ShouldNotContain("/health/live");
        sink.Eventos.ShouldAllBe(e => e.Level == LogEventLevel.Information);
    }

    /// <summary>
    /// Cria um ouvinte que amostra todas as atividades, para que o <see cref="Activity.Current"/> exista nos testes.
    /// </summary>
    /// <returns>Ouvinte já registrado.</returns>
    private static ActivityListener CriarOuvinte()
    {
        var ouvinte = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(ouvinte);
        return ouvinte;
    }
}
