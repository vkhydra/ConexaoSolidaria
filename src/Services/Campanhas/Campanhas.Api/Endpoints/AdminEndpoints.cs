using Campanhas.Application.Busca;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Operações administrativas (<c>/api/v1/admin</c>), exclusivas do papel <c>GestorONG</c>.
/// </summary>
public static class AdminEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /api/v1/admin/reindexar</c>.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearAdmin(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        var grupo = rotas.MapGroup("/api/v1/admin").RequireAuthorization(ExtensoesAutenticacao.PolicyGestorOng);
        grupo.MapPost("/reindexar", IniciarReindexacao);
        return rotas;
    }

    /// <summary>
    /// Inicia a reindexação das campanhas em segundo plano: 202 se começou, ou 409 se outra ainda roda.
    /// </summary>
    /// <param name="reindexar">Caso de uso da reindexação.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static IResult IniciarReindexacao(Reindexar reindexar)
    {
        var resultado = reindexar.Iniciar();
        return resultado.EhFalha ? ProblemasHttp.Para(resultado.Erro) : TypedResults.Accepted((string?)null);
    }
}
