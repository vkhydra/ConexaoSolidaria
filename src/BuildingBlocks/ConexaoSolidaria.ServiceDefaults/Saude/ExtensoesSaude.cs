using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.ServiceDefaults.Saude;

/// <summary>
/// Health checks padrão: <c>/health/live</c> (o processo responde) e <c>/health/ready</c>
/// (as dependências respondem).
/// </summary>
public static class ExtensoesSaude
{
    /// <summary>
    /// Rota da sonda de vida (<c>livenessProbe</c>): só confirma que o processo responde.
    /// </summary>
    public const string RotaVida = "/health/live";

    /// <summary>
    /// Rota da sonda de prontidão (<c>readinessProbe</c>): confere as dependências.
    /// </summary>
    public const string RotaProntidao = "/health/ready";

    /// <summary>
    /// Etiqueta que marca uma verificação como parte da prontidão.
    /// </summary>
    public const string EtiquetaProntidao = "ready";

    /// <summary>
    /// Registra o sistema de health checks. Cada serviço acrescenta as suas dependências com
    /// <see cref="EtiquetaProntidao"/> na lista de etiquetas.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    /// <returns>Construtor de health checks, para registrar as dependências do serviço.</returns>
    public static IHealthChecksBuilder AddSaude(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Services.AddHealthChecks();
    }

    /// <summary>
    /// Mapeia as rotas <c>/health/live</c> e <c>/health/ready</c>, com resposta em JSON.
    /// </summary>
    /// <param name="app">Aplicação web.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication MapearSaude(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(RotaVida, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = EscritorRespostaSaude.EscreverAsync,
        }).AllowAnonymous();

        app.MapHealthChecks(RotaProntidao, new HealthCheckOptions
        {
            Predicate = registro => registro.Tags.Contains(EtiquetaProntidao),
            ResponseWriter = EscritorRespostaSaude.EscreverAsync,
        }).AllowAnonymous();

        return app;
    }
}
