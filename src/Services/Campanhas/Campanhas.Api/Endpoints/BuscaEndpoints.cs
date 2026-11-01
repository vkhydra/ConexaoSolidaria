using Campanhas.Application.Busca;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Busca pública de campanhas (<c>/api/v1/campanhas/busca</c>), tolerante a erros de digitação.
/// </summary>
public static class BuscaEndpoints
{
    /// <summary>
    /// Mapeia <c>GET /api/v1/campanhas/busca</c>, público e sem autenticação. A rota literal tem precedência sobre
    /// <c>/api/v1/campanhas/{id}</c>, que exige o papel <c>GestorONG</c>.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearBusca(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        rotas.MapGet("/api/v1/campanhas/busca", BuscarAsync).AllowAnonymous();
        return rotas;
    }

    /// <summary>
    /// Busca campanhas ativas: 200 com a página (que pode ser vazia), 400 se o texto ou a paginação são inválidos, ou 503 se o
    /// Elasticsearch está fora do ar.
    /// </summary>
    /// <param name="q">Texto procurado, de 2 a 100 caracteres.</param>
    /// <param name="page">Número da página; 1 quando omitido.</param>
    /// <param name="pageSize">Itens por página; 10 quando omitido.</param>
    /// <param name="buscar">Caso de uso da busca.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> BuscarAsync(string? q, int? page, int? pageSize, BuscarCampanhas buscar, CancellationToken cancelamento)
    {
        var resultado = await buscar.ExecutarAsync(new BuscarCampanhasRequest(q, page ?? 1, pageSize ?? 10), cancelamento);
        return resultado.EhFalha ? ProblemasHttp.Para(resultado.Erro) : TypedResults.Ok(resultado.Valor);
    }
}
