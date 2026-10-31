using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ConexaoSolidaria.ServiceDefaults.Logs;

/// <summary>
/// Configuração de logs estruturados (JSON) para todos os serviços.
/// </summary>
public static class ExtensoesLogs
{
    /// <summary>
    /// Liga o Serilog com saída JSON compacta na saída padrão, enriquecida com serviço, versão,
    /// ambiente e identificadores do trace. Sinks registrados no contêiner de serviços (<c>ILogEventSink</c>) também recebem os
    /// eventos, o que permite aos testes conferir o que foi registrado.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    /// <param name="servico">Nome do serviço (ex.: <c>campanhas-api</c>).</param>
    /// <returns>O próprio construtor, para encadeamento.</returns>
    public static WebApplicationBuilder AddLogsEstruturados(this WebApplicationBuilder builder, string servico)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(servico);

        builder.Host.UseSerilog((contexto, servicos, configuracao) => configuracao
            .ConfigurarPadrao(servico, ObterVersao(), contexto.HostingEnvironment.EnvironmentName)
            .ReadFrom.Services(servicos)
            .WriteTo.Console(new RenderedCompactJsonFormatter()), preserveStaticLogger: true);

        return builder;
    }

    /// <summary>
    /// Registra um log por requisição HTTP, sem as sondas de saúde e de métricas.
    /// </summary>
    /// <param name="app">Aplicação web.</param>
    /// <param name="logger">Logger a usar; se for nulo, usa o logger do próprio host, configurado por <see cref="AddLogsEstruturados"/>.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication UsarLogsDeRequisicoes(this WebApplication app, Serilog.ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSerilogRequestLogging(opcoes =>
        {
            opcoes.GetLevel = NivelRequisicao.Obter;
            opcoes.Logger = logger ?? app.Services.GetRequiredService<Serilog.ILogger>();
        });
        return app;
    }

    /// <summary>
    /// Aplica o padrão do projeto: níveis mínimos, enriquecimento e propriedades fixas do serviço.
    /// A saída (sink) fica por conta de quem chama, o que permite testar a configuração.
    /// </summary>
    /// <param name="configuracao">Configuração do logger.</param>
    /// <param name="servico">Nome do serviço.</param>
    /// <param name="versao">Versão da aplicação.</param>
    /// <param name="ambiente">Nome do ambiente (Development, Production...).</param>
    /// <returns>A própria configuração, para encadeamento.</returns>
    public static LoggerConfiguration ConfigurarPadrao(this LoggerConfiguration configuracao, string servico, string versao, string ambiente)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        return configuracao
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<EnriquecedorTrace>()
            .Enrich.WithProperty("service", servico)
            .Enrich.WithProperty("version", versao)
            .Enrich.WithProperty("environment", ambiente);
    }

    /// <summary>
    /// Obtém a versão da aplicação a partir da variável <c>APP_VERSION</c> (tag da imagem).
    /// </summary>
    /// <returns>Texto com a versão.</returns>
    public static string ObterVersao()
    {
        return ObterVersao(Environment.GetEnvironmentVariable("APP_VERSION"));
    }

    /// <summary>
    /// Escolhe a versão a exibir: o valor informado ou, se estiver vazio, a versão do assembly de entrada.
    /// </summary>
    /// <param name="valorConfigurado">Valor da variável <c>APP_VERSION</c>, se existir.</param>
    /// <returns>Texto com a versão.</returns>
    public static string ObterVersao(string? valorConfigurado)
    {
        if (!string.IsNullOrWhiteSpace(valorConfigurado))
        {
            return valorConfigurado;
        }

        return VersaoDe(Assembly.GetEntryAssembly());
    }

    /// <summary>
    /// Lê a versão de um assembly.
    /// </summary>
    /// <param name="assembly">Assembly de entrada, que pode não existir (por exemplo, em alguns hosts de teste).</param>
    /// <returns>A versão do assembly ou <c>0.0.0</c> se não houver.</returns>
    public static string VersaoDe(Assembly? assembly)
    {
        return assembly?.GetName().Version?.ToString() ?? "0.0.0";
    }
}
