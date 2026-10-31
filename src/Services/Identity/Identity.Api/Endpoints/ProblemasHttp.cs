using ConexaoSolidaria.ServiceDefaults.Erros;
using Identity.Domain.Comum;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Identity.Api.Endpoints;

/// <summary>
/// Converte erros de negócio em respostas <c>application/problem+json</c>, com o código estável em <c>code</c>.
/// </summary>
public static class ProblemasHttp
{
    /// <summary>
    /// Cria a resposta 400 com os erros por campo.
    /// </summary>
    /// <param name="erro">Erro de validação, com <see cref="Erro.Campos"/> preenchido.</param>
    /// <returns>A resposta 400.</returns>
    public static ValidationProblem Validacao(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return TypedResults.ValidationProblem(
            erro.Campos!.ToDictionary(c => c.Key, c => c.Value),
            title: CatalogoDeErros.TituloPara(StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Cria uma resposta de erro com o status informado, o título igual à mensagem e o código estável.
    /// </summary>
    /// <param name="erro">Erro de negócio.</param>
    /// <param name="status">Status HTTP da resposta.</param>
    /// <returns>A resposta de erro.</returns>
    public static ProblemHttpResult Regra(Erro erro, int status)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return TypedResults.Problem(
            title: erro.Mensagem,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = erro.Codigo });
    }
}
