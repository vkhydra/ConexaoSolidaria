using System.Security.Claims;
using Campanhas.Application.Doacoes;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Endpoint de doação (<c>/api/v1/doacoes</c>), exclusivo do papel <c>Doador</c>.
/// </summary>
public static class DoacoesEndpoints
{
    /// <summary>
    /// Mapeia o recebimento de doação.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearDoacoes(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        var grupo = rotas.MapGroup("/api/v1/doacoes").RequireAuthorization(ExtensoesAutenticacao.PolicyDoador);
        grupo.MapPost("/", ReceberAsync);
        return rotas;
    }

    /// <summary>
    /// Recebe a doação: 202 com o identificador, ou 400, 404 ou 422. O total da campanha muda depois, quando o Worker processa.
    /// </summary>
    /// <param name="request">Corpo do pedido.</param>
    /// <param name="usuario">Doador autenticado, de onde vem o identificador (<c>sub</c>).</param>
    /// <param name="receber">Caso de uso de recebimento.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> ReceberAsync(DoacaoRequest request, ClaimsPrincipal usuario, ReceberDoacao receber, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(usuario.FindFirstValue("sub"), out var doadorId))
        {
            return TypedResults.Unauthorized();
        }

        var resultado = await receber.ExecutarAsync(doadorId, request, cancelamento);
        return resultado.EhSucesso ? TypedResults.Accepted((string?)null, resultado.Valor) : ProblemasHttp.Para(resultado.Erro);
    }
}
