using ConexaoSolidaria.ServiceDefaults.Logs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace ConexaoSolidaria.ServiceDefaults.Telemetria;

/// <summary>
/// Traces distribuídos (OpenTelemetry) e métricas (Prometheus) para todos os serviços.
/// </summary>
public static class ExtensoesTelemetria
{
    /// <summary>
    /// Rota em que o Prometheus coleta as métricas.
    /// </summary>
    public const string RotaMetricas = "/metrics";

    /// <summary>
    /// Nome da variável de ambiente padrão do OpenTelemetry com o endereço do coletor OTLP.
    /// </summary>
    public const string VariavelEndpointOtlp = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Caminhos que não geram trace: sondas de saúde e coleta de métricas.
    /// </summary>
    private static readonly string[] _caminhosSemTrace = ["/health", RotaMetricas];

    /// <summary>
    /// Liga os traces do OpenTelemetry com o serviço, a versão e o ambiente como atributos do recurso.
    /// O envio ao coletor (Jaeger) só é ligado se <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> estiver definido.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    /// <param name="servico">Nome do serviço (<c>service.name</c>).</param>
    /// <param name="fontesAdicionais">Fontes de atividade extras a registrar (por exemplo, <c>MassTransit</c>).</param>
    /// <returns>O próprio construtor, para encadeamento.</returns>
    public static WebApplicationBuilder AddTelemetria(this WebApplicationBuilder builder, string servico, params string[] fontesAdicionais)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(servico);

        var opentelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(recurso => recurso
                .AddService(servico, serviceVersion: ExtensoesLogs.ObterVersao())
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)]))
            .WithTracing(traces =>
            {
                traces.AddAspNetCoreInstrumentation(opcoes => opcoes.Filter = contexto => !CaminhoSemTrace(contexto.Request.Path));
                traces.AddHttpClientInstrumentation();
                if (fontesAdicionais.Length > 0)
                {
                    traces.AddSource(fontesAdicionais);
                }
            });

        if (OtlpConfigurado(builder.Configuration))
        {
            opentelemetry.UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Expõe as métricas HTTP e do processo em <c>/metrics</c>, no formato do Prometheus.
    /// </summary>
    /// <param name="app">Aplicação web.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication MapearMetricas(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHttpMetrics();
        app.MapMetrics(RotaMetricas);
        return app;
    }

    /// <summary>
    /// Indica se há um coletor OTLP configurado.
    /// </summary>
    /// <param name="configuracao">Configuração da aplicação (inclui variáveis de ambiente).</param>
    /// <returns><c>true</c> se <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> tem valor.</returns>
    public static bool OtlpConfigurado(IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        return !string.IsNullOrWhiteSpace(configuracao[VariavelEndpointOtlp]);
    }

    /// <summary>
    /// Indica se o caminho é de sonda ou de métricas e, por isso, não deve gerar trace.
    /// </summary>
    /// <param name="caminho">Caminho da requisição.</param>
    /// <returns><c>true</c> para <c>/health</c> e <c>/metrics</c>.</returns>
    public static bool CaminhoSemTrace(PathString caminho)
    {
        foreach (var excluido in _caminhosSemTrace)
        {
            if (caminho.StartsWithSegments(excluido, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
