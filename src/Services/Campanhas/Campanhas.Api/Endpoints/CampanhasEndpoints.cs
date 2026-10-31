using Campanhas.Application.Campanhas;
using Campanhas.Domain.Campanhas;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Endpoints de gestão de campanhas (<c>/api/v1/campanhas</c>), exclusivos do papel <c>GestorONG</c>.
/// </summary>
public static class CampanhasEndpoints
{
    /// <summary>
    /// Mapeia criação, listagem e detalhe de campanhas.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearCampanhas(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        var grupo = rotas.MapGroup("/api/v1/campanhas").RequireAuthorization(ExtensoesAutenticacao.PolicyGestorOng);
        grupo.MapPost("/", CriarAsync);
        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:guid}", ObterAsync);
        return rotas;
    }

    /// <summary>
    /// Cria a campanha: 201 com <c>Location</c> e <c>ETag</c>, 400 ou 422.
    /// </summary>
    /// <param name="request">Corpo do pedido.</param>
    /// <param name="criar">Caso de uso de criação.</param>
    /// <param name="resposta">Resposta HTTP, para o cabeçalho ETag.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> CriarAsync(CampanhaRequest request, CriarCampanha criar, HttpResponse resposta, CancellationToken cancelamento)
    {
        var resultado = await criar.ExecutarAsync(request, cancelamento);
        if (resultado.EhFalha)
        {
            return ProblemasHttp.Para(resultado.Erro);
        }

        resposta.Headers.ETag = ProblemasHttp.ETag(resultado.Valor.Versao);
        return TypedResults.Created($"/api/v1/campanhas/{resultado.Valor.Id}", resultado.Valor);
    }

    /// <summary>
    /// Lista campanhas com filtro de status e paginação: 200 ou 400.
    /// </summary>
    /// <param name="status">Status a filtrar.</param>
    /// <param name="page">Número da página.</param>
    /// <param name="pageSize">Itens por página.</param>
    /// <param name="listar">Caso de uso de listagem.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> ListarAsync(string? status, int? page, int? pageSize, ListarCampanhas listar, CancellationToken cancelamento)
    {
        var resultado = await listar.ExecutarAsync(new ListarCampanhasRequest(status, page ?? 1, pageSize ?? 20), cancelamento);
        return resultado.EhSucesso ? TypedResults.Ok(resultado.Valor) : ProblemasHttp.Para(resultado.Erro);
    }

    /// <summary>
    /// Detalha uma campanha: 200 com <c>ETag</c> ou 404.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="obter">Caso de uso de detalhe.</param>
    /// <param name="resposta">Resposta HTTP, para o cabeçalho ETag.</param>
    /// <param name="cancelamento">Token de cancelamento da requisição.</param>
    /// <returns>A resposta HTTP correspondente ao resultado.</returns>
    private static async Task<IResult> ObterAsync(Guid id, ObterCampanha obter, HttpResponse resposta, CancellationToken cancelamento)
    {
        var resultado = await obter.ExecutarAsync(new CampanhaId(id), cancelamento);
        if (resultado.EhFalha)
        {
            return ProblemasHttp.Para(resultado.Erro);
        }

        resposta.Headers.ETag = ProblemasHttp.ETag(resultado.Valor.Versao);
        return TypedResults.Ok(resultado.Valor);
    }
}
