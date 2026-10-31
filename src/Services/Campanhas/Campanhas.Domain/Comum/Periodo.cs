namespace Campanhas.Domain.Comum;

/// <summary>
/// Intervalo de datas de uma campanha, com início e fim inclusivos (a data fim é válida até o fim do dia).
/// </summary>
public sealed record Periodo
{
    /// <summary>
    /// Código do erro de período inválido.
    /// </summary>
    public const string CodigoInvalido = "campanha.periodo_invalido";

    /// <summary>
    /// Cria o período já validado.
    /// </summary>
    /// <param name="dataInicio">Primeiro dia.</param>
    /// <param name="dataFim">Último dia.</param>
    private Periodo(DateOnly dataInicio, DateOnly dataFim)
    {
        DataInicio = dataInicio;
        DataFim = dataFim;
    }

    /// <summary>
    /// Obtém o primeiro dia do período.
    /// </summary>
    public DateOnly DataInicio { get; }

    /// <summary>
    /// Obtém o último dia do período.
    /// </summary>
    public DateOnly DataFim { get; }

    /// <summary>
    /// Cria um período em que a data fim não é anterior à data início.
    /// </summary>
    /// <param name="dataInicio">Primeiro dia.</param>
    /// <param name="dataFim">Último dia.</param>
    /// <returns>O período ou o erro <c>campanha.periodo_invalido</c>.</returns>
    public static Result<Periodo> Criar(DateOnly dataInicio, DateOnly dataFim)
    {
        if (dataFim < dataInicio)
        {
            return Result.Falha<Periodo>(new Erro(CodigoInvalido, "A data fim não pode ser anterior à data início."));
        }

        return Result.Sucesso(new Periodo(dataInicio, dataFim));
    }

    /// <summary>
    /// Informa se um dia está dentro do período, contando o primeiro e o último.
    /// </summary>
    /// <param name="dia">Dia a conferir.</param>
    /// <returns><c>true</c> se o dia está entre a data início e a data fim, inclusive.</returns>
    public bool Contem(DateOnly dia)
    {
        return dia >= DataInicio && dia <= DataFim;
    }
}
