namespace Campanhas.Domain.Comum;

/// <summary>
/// Valor em reais com duas casas decimais, nunca negativo. Valores com mais casas são arredondados pelo arredondamento
/// bancário (para o par mais próximo).
/// </summary>
public sealed record Dinheiro
{
    /// <summary>
    /// Código do erro de valor negativo.
    /// </summary>
    public const string CodigoNegativo = "dinheiro.negativo";

    /// <summary>
    /// Quantidade de casas decimais de um valor em reais.
    /// </summary>
    public const int CasasDecimais = 2;

    /// <summary>
    /// Cria o valor já validado e arredondado.
    /// </summary>
    /// <param name="valor">Valor com duas casas.</param>
    private Dinheiro(decimal valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Obtém o valor zero.
    /// </summary>
    public static Dinheiro Zero { get; } = new(0m);

    /// <summary>
    /// Obtém o valor em reais, com duas casas decimais.
    /// </summary>
    public decimal Valor { get; }

    /// <summary>
    /// Cria um valor em reais, arredondando para duas casas.
    /// </summary>
    /// <param name="valor">Valor em reais.</param>
    /// <returns>O valor ou o erro <c>dinheiro.negativo</c>.</returns>
    public static Result<Dinheiro> Criar(decimal valor)
    {
        var arredondado = decimal.Round(valor, CasasDecimais, MidpointRounding.ToEven);
        if (arredondado < 0m)
        {
            return Result.Falha<Dinheiro>(new Erro(CodigoNegativo, "O valor não pode ser negativo."));
        }

        return Result.Sucesso(new Dinheiro(arredondado));
    }

    /// <summary>
    /// Soma outro valor a este.
    /// </summary>
    /// <param name="outro">Valor a somar.</param>
    /// <returns>A soma, com duas casas.</returns>
    public Dinheiro Somar(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);

        return new Dinheiro(Valor + outro.Valor);
    }

    /// <summary>
    /// Devolve o valor em texto invariante, com duas casas.
    /// </summary>
    /// <returns>O valor, como em <c>1000.00</c>.</returns>
    public override string ToString()
    {
        return Valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}
