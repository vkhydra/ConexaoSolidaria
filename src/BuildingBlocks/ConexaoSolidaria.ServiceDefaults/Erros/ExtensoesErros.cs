using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.ServiceDefaults.Erros;

/// <summary>
/// Tratamento global de erros: toda resposta de erro vira <c>application/problem+json</c>
/// com <c>code</c> estável e <c>traceId</c>.
/// </summary>
public static class ExtensoesErros
{
    /// <summary>
    /// Registra o <c>ProblemDetails</c> padrão do projeto e o tratador de exceções.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    /// <returns>O próprio construtor, para encadeamento.</returns>
    public static WebApplicationBuilder AddTratamentoErros(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = Completar);
        builder.Services.AddExceptionHandler<TratadorExcecoes>();
        return builder;
    }

    /// <summary>
    /// Liga o tratador de exceções e a conversão de respostas vazias de erro (401, 403, 404...) em <c>ProblemDetails</c>.
    /// Deve ser chamado no início do pipeline.
    /// </summary>
    /// <param name="app">Aplicação web.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication UsarTratamentoErros(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    /// <summary>
    /// Completa toda resposta de erro com <c>traceId</c>, <c>code</c>, <c>type</c> e título em português,
    /// preservando o <c>code</c> quando a regra de negócio já definiu um.
    /// </summary>
    /// <param name="contexto">Contexto da resposta de erro.</param>
    private static void Completar(ProblemDetailsContext contexto)
    {
        var problema = contexto.ProblemDetails;
        var status = problema.Status ?? contexto.HttpContext.Response.StatusCode;

        problema.Status = status;
        problema.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? contexto.HttpContext.TraceIdentifier;

        if (!problema.Extensions.TryGetValue("code", out var codigoExistente) || codigoExistente is null)
        {
            problema.Extensions["code"] = CatalogoDeErros.CodigoPara(status);
        }

        problema.Type = CatalogoDeErros.PrefixoTipo + problema.Extensions["code"];
        problema.Instance ??= contexto.HttpContext.Request.Path;

        if (TituloEhPadraoDoFramework(problema.Title, status))
        {
            problema.Title = CatalogoDeErros.TituloPara(status);
        }
    }

    /// <summary>
    /// Indica se o título está vazio ou é um dos textos padrão, em inglês, que o ASP.NET Core coloca sozinho.
    /// Títulos escritos pela regra de negócio são preservados.
    /// </summary>
    /// <param name="titulo">Título atual da resposta.</param>
    /// <param name="status">Status HTTP da resposta.</param>
    /// <returns><c>true</c> se o título deve ser trocado pelo texto em português do catálogo.</returns>
    private static bool TituloEhPadraoDoFramework(string? titulo, int status)
    {
        return string.IsNullOrWhiteSpace(titulo)
            || titulo == ReasonPhrases.GetReasonPhrase(status)
            || titulo == "An error occurred while processing your request.";
    }
}
