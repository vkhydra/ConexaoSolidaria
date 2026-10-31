using System.Security.Claims;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using Identity.Application.Titular;
using Identity.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Identity.Api.Endpoints;

/// <summary>
/// Direitos do titular da LGPD (<c>/api/v1/doadores/me</c>): acesso aos próprios dados e eliminação por anonimização.
/// Exigem o papel <c>Doador</c>; as respostas levam <c>Cache-Control: no-store</c> porque contêm dado pessoal.
/// </summary>
public static class TitularEndpoints
{
    /// <summary>
    /// Mapeia <c>GET</c> e <c>DELETE /api/v1/doadores/me</c>.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearTitular(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        var grupo = rotas.MapGroup("/api/v1/doadores/me")
            .RequireAuthorization(ExtensoesAutenticacao.PolicyDoador)
            .AddEndpointFilter(async (contexto, proximo) =>
            {
                contexto.HttpContext.Response.Headers.CacheControl = "no-store";
                return await proximo(contexto);
            });

        grupo.MapGet("/", ObterAsync);
        grupo.MapDelete("/", AnonimizarAsync);
        return rotas;
    }

    /// <summary>
    /// Devolve os dados do doador autenticado: 200 ou 401 se o usuário não existe mais ou foi anonimizado.
    /// </summary>
    /// <param name="usuario">Usuário autenticado, vindo do token.</param>
    /// <param name="obter">Caso de uso de acesso aos dados.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<Results<Ok<DoadorResponse>, ProblemHttpResult>> ObterAsync(
        ClaimsPrincipal usuario,
        ObterMeusDados obter,
        CancellationToken cancelamento)
    {
        if (!TentarObterId(usuario, out var id))
        {
            return ProblemasHttp.Regra(ErrosDoTitular.NaoAutenticado, StatusCodes.Status401Unauthorized);
        }

        var resultado = await obter.ExecutarAsync(id, cancelamento);
        return resultado.EhSucesso
            ? TypedResults.Ok(resultado.Valor)
            : ProblemasHttp.Regra(resultado.Erro, StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Anonimiza o cadastro do doador autenticado: 204 ou 401 se o usuário não existe mais ou já foi anonimizado.
    /// </summary>
    /// <param name="usuario">Usuário autenticado, vindo do token.</param>
    /// <param name="anonimizar">Caso de uso de anonimização.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> AnonimizarAsync(
        ClaimsPrincipal usuario,
        AnonimizarMeusDados anonimizar,
        CancellationToken cancelamento)
    {
        if (!TentarObterId(usuario, out var id))
        {
            return ProblemasHttp.Regra(ErrosDoTitular.NaoAutenticado, StatusCodes.Status401Unauthorized);
        }

        var resultado = await anonimizar.ExecutarAsync(id, cancelamento);
        return resultado.EhSucesso
            ? TypedResults.NoContent()
            : ProblemasHttp.Regra(resultado.Erro, StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Lê o identificador do usuário na claim <c>sub</c> do token.
    /// </summary>
    /// <param name="usuario">Usuário autenticado.</param>
    /// <param name="id">Identificador lido, quando a claim é um GUID.</param>
    /// <returns><c>true</c> se a claim existe e é um GUID.</returns>
    private static bool TentarObterId(ClaimsPrincipal usuario, out UsuarioId id)
    {
        if (Guid.TryParse(usuario.FindFirstValue("sub"), out var guid))
        {
            id = new UsuarioId(guid);
            return true;
        }

        id = default;
        return false;
    }
}
