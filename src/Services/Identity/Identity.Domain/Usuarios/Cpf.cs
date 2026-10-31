using System.Text.RegularExpressions;
using Identity.Domain.Comum;

namespace Identity.Domain.Usuarios;

/// <summary>
/// CPF válido, guardado só com os 11 dígitos. Aceita a entrada com ou sem máscara, confere os dígitos verificadores
/// e rejeita sequências repetidas. É dado pessoal: <see cref="ToString"/> devolve a forma mascarada.
/// </summary>
public sealed partial record Cpf
{
    /// <summary>
    /// Código do erro de CPF inválido.
    /// </summary>
    public const string CodigoInvalido = "cpf.invalido";

    /// <summary>
    /// Quantidade de dígitos de um CPF.
    /// </summary>
    public const int QuantidadeDeDigitos = 11;

    /// <summary>
    /// Cria o CPF já validado.
    /// </summary>
    /// <param name="valor">Os 11 dígitos, sem máscara.</param>
    private Cpf(string valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Obtém os 11 dígitos, sem máscara. Use só para cifrar e calcular o hash; nunca para log.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Valida um CPF digitado com ou sem máscara (<c>529.982.247-25</c> ou <c>52998224725</c>).
    /// </summary>
    /// <param name="texto">Texto digitado pelo usuário.</param>
    /// <returns>O CPF só com dígitos ou o erro <c>cpf.invalido</c>.</returns>
    public static Result<Cpf> Criar(string? texto)
    {
        if (string.IsNullOrEmpty(texto) || !Formato().IsMatch(texto))
        {
            return Falha();
        }

        var digitos = texto.Replace(".", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (digitos.Distinct().Count() == 1 || !DigitosVerificadoresConferem(digitos))
        {
            return Falha();
        }

        return Result.Sucesso(new Cpf(digitos));
    }

    /// <summary>
    /// Devolve o CPF com os três primeiros dígitos e os dois últimos ocultos, como em <c>***.982.247-**</c>.
    /// </summary>
    /// <returns>O CPF mascarado, seguro para respostas e telas.</returns>
    public string Mascarado()
    {
        return $"***.{Valor.AsSpan(3, 3)}.{Valor.AsSpan(6, 3)}-**";
    }

    /// <summary>
    /// Devolve a forma mascarada, para que o CPF completo nunca vá parar em log por acidente.
    /// </summary>
    /// <returns>O CPF mascarado.</returns>
    public override string ToString()
    {
        return Mascarado();
    }

    /// <summary>
    /// Cria o resultado de falha padrão, sem repetir o valor digitado.
    /// </summary>
    /// <returns>Resultado de falha com o código <c>cpf.invalido</c>.</returns>
    private static Result<Cpf> Falha()
    {
        return Result.Falha<Cpf>(new Erro(CodigoInvalido, "O CPF informado não é válido."));
    }

    /// <summary>
    /// Confere os dois dígitos verificadores pelo módulo 11.
    /// </summary>
    /// <param name="digitos">Os 11 dígitos.</param>
    /// <returns><c>true</c> se os dois últimos dígitos correspondem aos nove primeiros.</returns>
    private static bool DigitosVerificadoresConferem(string digitos)
    {
        var primeiro = CalcularDigito(digitos, 9);
        var segundo = CalcularDigito(digitos, 10);

        return digitos[9] - '0' == primeiro && digitos[10] - '0' == segundo;
    }

    /// <summary>
    /// Calcula um dígito verificador com pesos decrescentes a partir de <c>quantidade + 1</c>.
    /// </summary>
    /// <param name="digitos">Os dígitos do CPF.</param>
    /// <param name="quantidade">Quantos dígitos entram na conta (9 para o primeiro, 10 para o segundo).</param>
    /// <returns>O dígito verificador, de 0 a 9.</returns>
    private static int CalcularDigito(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
        {
            soma += (digitos[i] - '0') * (quantidade + 1 - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    /// <summary>
    /// Expressão regular que aceita 11 dígitos, com ou sem os pontos e o hífen da máscara.
    /// </summary>
    /// <returns>A expressão regular compilada na geração de código.</returns>
    [GeneratedRegex(@"^(\d{11}|\d{3}\.\d{3}\.\d{3}-\d{2})$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Formato();
}
