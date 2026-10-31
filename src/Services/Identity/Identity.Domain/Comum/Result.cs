namespace Identity.Domain.Comum;

/// <summary>
/// Resultado de uma operação sem valor de retorno: sucesso ou falha com um <see cref="Erro"/>.
/// Erros de negócio são devolvidos assim, nunca como exceção.
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Resultado de sucesso compartilhado.
    /// </summary>
    private static readonly Result _sucesso = new(null);

    /// <summary>
    /// Erro da falha; nulo quando é sucesso.
    /// </summary>
    private readonly Erro? _erro;

    /// <summary>
    /// Cria o resultado.
    /// </summary>
    /// <param name="erro">Erro da falha, ou nulo para sucesso.</param>
    private Result(Erro? erro)
    {
        _erro = erro;
    }

    /// <summary>
    /// Obtém um valor que indica se a operação teve sucesso.
    /// </summary>
    public bool EhSucesso => _erro is null;

    /// <summary>
    /// Obtém um valor que indica se a operação falhou.
    /// </summary>
    public bool EhFalha => _erro is not null;

    /// <summary>
    /// Obtém o erro da falha.
    /// </summary>
    /// <exception cref="InvalidOperationException">O resultado é de sucesso.</exception>
    public Erro Erro => _erro ?? throw new InvalidOperationException("Um resultado de sucesso não tem erro.");

    /// <summary>
    /// Cria um resultado de sucesso sem valor.
    /// </summary>
    /// <returns>O resultado de sucesso.</returns>
    public static Result Ok()
    {
        return _sucesso;
    }

    /// <summary>
    /// Cria um resultado de falha sem valor.
    /// </summary>
    /// <param name="erro">Erro que causou a falha.</param>
    /// <returns>O resultado de falha.</returns>
    public static Result Falha(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return new Result(erro);
    }

    /// <summary>
    /// Cria um resultado de sucesso com valor.
    /// </summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="valor">Valor produzido pela operação.</param>
    /// <returns>O resultado de sucesso com o valor.</returns>
    public static Result<T> Sucesso<T>(T valor)
    {
        return new Result<T>(valor, null);
    }

    /// <summary>
    /// Cria um resultado de falha com tipo de valor.
    /// </summary>
    /// <typeparam name="T">Tipo do valor que a operação produziria.</typeparam>
    /// <param name="erro">Erro que causou a falha.</param>
    /// <returns>O resultado de falha.</returns>
    public static Result<T> Falha<T>(Erro erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return new Result<T>(default, erro);
    }
}
