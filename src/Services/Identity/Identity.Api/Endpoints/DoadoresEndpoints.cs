using Identity.Application.Doadores;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Identity.Api.Endpoints;

/// <summary>
/// Endpoints de doadores (<c>/api/v1/doadores</c>).
/// </summary>
public static class DoadoresEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /api/v1/doadores</c>, público: cadastro de doador.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearDoadores(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        rotas.MapGroup("/api/v1/doadores")
            .MapPost("/", CadastrarAsync)
            .AllowAnonymous();

        return rotas;
    }

    /// <summary>
    /// Cadastra o doador e traduz o resultado em resposta HTTP: 201, 400 ou 409.
    /// </summary>
    /// <param name="request">Corpo do cadastro.</param>
    /// <param name="cadastrar">Caso de uso de cadastro.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<Results<Created<CadastrarDoadorResponse>, ValidationProblem, ProblemHttpResult>> CadastrarAsync(
        CadastrarDoadorRequest request,
        CadastrarDoador cadastrar,
        CancellationToken cancelamento)
    {
        var resultado = await cadastrar.ExecutarAsync(request, cancelamento);
        if (resultado.EhSucesso)
        {
            return TypedResults.Created("/api/v1/doadores/me", resultado.Valor);
        }

        var erro = resultado.Erro;
        return erro.Codigo switch
        {
            CadastrarDoador.CodigoEmailEmUso or CadastrarDoador.CodigoCpfEmUso => ProblemasHttp.Regra(erro, StatusCodes.Status409Conflict),
            _ => ProblemasHttp.Validacao(erro),
        };
    }
}
