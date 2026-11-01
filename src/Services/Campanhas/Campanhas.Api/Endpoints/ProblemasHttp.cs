using Campanhas.Application.Busca;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Domain.Comum;
using ConexaoSolidaria.ServiceDefaults.Erros;

namespace Campanhas.Api.Endpoints;

/// <summary>
/// Converte erros de negócio em respostas <c>application/problem+json</c>, com o código estável em <c>code</c>.
/// </summary>
public static class ProblemasHttp
{
    /// <summary>
    /// Escolhe o status HTTP de um erro de negócio: 400 validação, 404 inexistente, 409 conflito de versão ou reindexação em andamento,
    /// 412 versão ausente, 503 busca indisponível e 422 para as demais regras.
    /// </summary>
    /// <param name="erro">Erro de negócio.</param>
    /// <returns>O status HTTP correspondente.</returns>
    public static int StatusDe(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return erro.Codigo switch
        {
            ErrosDeValidacao.CodigoValidacao => StatusCodes.Status400BadRequest,
            ErrosDeCampanha.CodigoNaoEncontrada => StatusCodes.Status404NotFound,
            ErrosDeCampanha.CodigoConflitoDeVersao => StatusCodes.Status409Conflict,
            ErrosDeCampanha.CodigoVersaoObrigatoria => StatusCodes.Status412PreconditionFailed,
            ErrosDeBusca.CodigoIndisponivel => StatusCodes.Status503ServiceUnavailable,
            ErrosDeBusca.CodigoReindexacaoEmAndamento => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
    }

    /// <summary>
    /// Cria a resposta de erro de um <see cref="Erro"/>: 400 com os campos, ou o status escolhido por <see cref="StatusDe"/>.
    /// </summary>
    /// <param name="erro">Erro de negócio.</param>
    /// <returns>A resposta de erro.</returns>
    public static IResult Para(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        if (erro.Codigo == ErrosDeValidacao.CodigoValidacao)
        {
            return TypedResults.ValidationProblem(
                erro.Campos!.ToDictionary(c => c.Key, c => c.Value),
                title: CatalogoDeErros.TituloPara(StatusCodes.Status400BadRequest));
        }

        return TypedResults.Problem(
            title: erro.Mensagem,
            statusCode: StatusDe(erro),
            extensions: new Dictionary<string, object?> { ["code"] = erro.Codigo });
    }

    /// <summary>
    /// Formata a versão da campanha como valor do cabeçalho <c>ETag</c> (entre aspas).
    /// </summary>
    /// <param name="versao">Versão da campanha.</param>
    /// <returns>O valor do ETag, como em <c>"7"</c>.</returns>
    public static string ETag(uint versao)
    {
        return $"\"{versao}\"";
    }
}
