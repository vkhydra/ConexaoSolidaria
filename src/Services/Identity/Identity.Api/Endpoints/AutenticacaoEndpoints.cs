using Identity.Application.Autenticacao;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Identity.Api.Endpoints;

/// <summary>
/// Endpoints de autenticação (<c>/api/v1/auth</c>).
/// </summary>
public static class AutenticacaoEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /api/v1/auth/login</c>, público.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearAutenticacao(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        rotas.MapGroup("/api/v1/auth")
            .MapPost("/login", LoginAsync)
            .AllowAnonymous();

        return rotas;
    }

    /// <summary>
    /// Executa o login e traduz o resultado em resposta HTTP: 200, 400, 401 ou 429.
    /// </summary>
    /// <param name="request">Corpo do login.</param>
    /// <param name="autenticar">Caso de uso de autenticação.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<Results<Ok<AutenticarResponse>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        AutenticarRequest request,
        Autenticar autenticar,
        CancellationToken cancelamento)
    {
        var resultado = await autenticar.ExecutarAsync(request, cancelamento);
        if (resultado.EhSucesso)
        {
            return TypedResults.Ok(resultado.Valor);
        }

        var erro = resultado.Erro;
        if (erro.Codigo == Autenticar.CodigoValidacao)
        {
            return ProblemasHttp.Validacao(erro);
        }

        var status = erro.Codigo == Autenticar.CodigoBloqueado
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status401Unauthorized;

        return ProblemasHttp.Regra(erro, status);
    }
}
