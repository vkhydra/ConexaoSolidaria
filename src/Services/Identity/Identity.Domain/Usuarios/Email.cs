using System.Text.RegularExpressions;
using Identity.Domain.Comum;

namespace Identity.Domain.Usuarios;

/// <summary>
/// Endereço de e-mail válido e normalizado em minúsculas. Dois e-mails são iguais quando o texto normalizado é igual.
/// </summary>
public sealed partial record Email
{
    /// <summary>
    /// Código do erro de e-mail inválido.
    /// </summary>
    public const string CodigoInvalido = "email.invalido";

    /// <summary>
    /// Tamanho máximo de um endereço de e-mail (RFC 5321).
    /// </summary>
    public const int TamanhoMaximo = 254;

    /// <summary>
    /// Cria o e-mail já validado.
    /// </summary>
    /// <param name="valor">Texto normalizado em minúsculas.</param>
    private Email(string valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Obtém o endereço normalizado em minúsculas.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Valida e normaliza um endereço de e-mail. Espaços nas pontas ou no meio tornam o e-mail inválido.
    /// </summary>
    /// <param name="texto">Texto digitado pelo usuário.</param>
    /// <returns>O e-mail em minúsculas ou o erro <c>email.invalido</c>.</returns>
    public static Result<Email> Criar(string? texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length > TamanhoMaximo || !Formato().IsMatch(texto) || texto.Contains("..", StringComparison.Ordinal))
        {
            return Result.Falha<Email>(new Erro(CodigoInvalido, "O e-mail informado não é válido."));
        }

        return Result.Sucesso(new Email(texto.ToLowerInvariant()));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Valor;
    }

    /// <summary>
    /// Expressão regular do formato básico: parte local, arroba e domínio com ponto, sem espaços.
    /// </summary>
    /// <returns>A expressão regular compilada na geração de código.</returns>
    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Formato();
}
