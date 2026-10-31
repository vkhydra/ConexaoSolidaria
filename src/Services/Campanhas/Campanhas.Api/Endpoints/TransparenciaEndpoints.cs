using Campanhas.Application.Transparencia;
using Npgsql;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Painel público de transparência (<c>/api/v1/transparencia/campanhas</c>): campanhas ativas, meta e total arrecadado.
/// </summary>
public static class TransparenciaEndpoints
{
    /// <summary>
    /// Código do erro devolvido quando o banco está indisponível.
    /// </summary>
    public const string CodigoServicoIndisponivel = "servico.indisponivel";

    /// <summary>
    /// Mapeia <c>GET /api/v1/transparencia/campanhas</c>, público e sem autenticação.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearTransparencia(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        rotas.MapGet("/api/v1/transparencia/campanhas", ObterPainelAsync).AllowAnonymous();
        return rotas;
    }

    /// <summary>
    /// Devolve o painel: 200 com a lista (que pode ser vazia) e <c>Cache-Control: public, max-age=10</c>, ou 503 se o banco não responde.
    /// </summary>
    /// <param name="obter">Caso de uso do painel.</param>
    /// <param name="resposta">Resposta HTTP, para o cabeçalho de cache.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> ObterPainelAsync(ObterPainelTransparencia obter, HttpResponse resposta, CancellationToken cancelamento)
    {
        try
        {
            var painel = await obter.ExecutarAsync(cancelamento);
            resposta.Headers.CacheControl = "public, max-age=10";
            return TypedResults.Ok(painel);
        }
        catch (Exception excecao) when (excecao is NpgsqlException or TimeoutException)
        {
            return TypedResults.Problem(
                title: "O painel está temporariamente indisponível.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = CodigoServicoIndisponivel });
        }
    }
}
