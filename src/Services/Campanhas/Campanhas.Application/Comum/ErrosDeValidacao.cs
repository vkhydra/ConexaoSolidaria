using Campanhas.Domain.Comum;
using FluentValidation.Results;

namespace Campanhas.Application.Comum;

/// <summary>
/// Converte o resultado do FluentValidation no erro de negócio <c>validacao</c>, com os nomes dos campos em camelCase
/// como no JSON da API. As mensagens nunca repetem o valor digitado.
/// </summary>
public static class ErrosDeValidacao
{
    /// <summary>
    /// Código do erro de entrada inválida.
    /// </summary>
    public const string CodigoValidacao = "validacao";

    /// <summary>
    /// Cria o erro <c>validacao</c> com as mensagens agrupadas por campo.
    /// </summary>
    /// <param name="resultado">Resultado da validação, com ao menos um erro.</param>
    /// <returns>O erro, com <see cref="Erro.Campos"/> preenchido.</returns>
    public static Erro Converter(ValidationResult resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        var campos = resultado.Errors
            .GroupBy(e => CamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new Erro(CodigoValidacao, "Dados inválidos.") { Campos = campos };
    }

    /// <summary>
    /// Passa a primeira letra do nome da propriedade para minúscula.
    /// </summary>
    /// <param name="nome">Nome da propriedade (por exemplo, <c>DataFim</c>).</param>
    /// <returns>O nome em camelCase (por exemplo, <c>dataFim</c>).</returns>
    private static string CamelCase(string nome)
    {
        return string.IsNullOrEmpty(nome) ? nome : char.ToLowerInvariant(nome[0]) + nome[1..];
    }
}
