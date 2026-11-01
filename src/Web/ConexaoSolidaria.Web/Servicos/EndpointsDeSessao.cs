using Microsoft.AspNetCore.Antiforgery;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Endpoints que encerram a sessão: <c>POST /sair</c> (botão Sair do menu) e <c>GET /sessao-encerrada</c> (depois de excluir os dados).
/// </summary>
public static class EndpointsDeSessao
{
    /// <summary>
    /// Mapeia <c>POST /sair</c> e <c>GET /sessao-encerrada</c>.
    /// </summary>
    /// <param name="rotas">Construtor de rotas da aplicação.</param>
    /// <returns>O próprio construtor de rotas, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapearSessao(this IEndpointRouteBuilder rotas)
    {
        ArgumentNullException.ThrowIfNull(rotas);

        rotas.MapPost("/sair", SairAsync).AllowAnonymous();
        rotas.MapGet("/sessao-encerrada", EncerrarAposExclusaoAsync).AllowAnonymous();
        return rotas;
    }

    /// <summary>
    /// Valida o token antifalsificação do formulário, apaga o cookie e volta ao início. Sem token válido, responde 400 e a
    /// sessão continua (impede que outro site deslogue o usuário).
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição.</param>
    /// <param name="antiforgery">Serviço antifalsificação.</param>
    /// <param name="sessao">Serviço de sessão.</param>
    /// <returns>Redirecionamento ao início, ou 400.</returns>
    public static async Task<IResult> SairAsync(HttpContext contexto, IAntiforgery antiforgery, IServicoDeSessao sessao)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(contexto);
        }
        catch (AntiforgeryValidationException)
        {
            return TypedResults.BadRequest();
        }

        await sessao.SairAsync(contexto);
        return TypedResults.Redirect("/");
    }

    /// <summary>
    /// Apaga o cookie e volta ao início com o aviso de conta excluída. Só encerra a sessão (não altera dados), então é seguro
    /// responder a um GET.
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição.</param>
    /// <param name="sessao">Serviço de sessão.</param>
    /// <returns>Redirecionamento ao início.</returns>
    public static async Task<IResult> EncerrarAposExclusaoAsync(HttpContext contexto, IServicoDeSessao sessao)
    {
        await sessao.SairAsync(contexto);
        return TypedResults.Redirect("/?excluida=true");
    }
}
