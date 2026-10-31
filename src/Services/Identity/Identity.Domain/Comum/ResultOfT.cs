namespace Identity.Domain.Comum;

/// <summary>
/// Resultado de uma operação que produz um valor: sucesso com o valor ou falha com um <see cref="Erro"/>.
/// Criado por <see cref="Result.Sucesso{T}(T)"/> e <see cref="Result.Falha{T}(Erro)"/>.
/// </summary>
/// <typeparam name="T">Tipo do valor produzido.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// Valor produzido; só existe no sucesso.
    /// </summary>
    private readonly T? _valor;

    /// <summary>
    /// Erro da falha; nulo no sucesso.
    /// </summary>
    private readonly Erro? _erro;

    /// <summary>
    /// Cria o resultado.
    /// </summary>
    /// <param name="valor">Valor do sucesso.</param>
    /// <param name="erro">Erro da falha, ou nulo para sucesso.</param>
    internal Result(T? valor, Erro? erro)
    {
        _valor = valor;
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
    /// Obtém o valor produzido.
    /// </summary>
    /// <exception cref="InvalidOperationException">O resultado é de falha.</exception>
    public T Valor => _erro is null ? _valor! : throw new InvalidOperationException("Um resultado de falha não tem valor.");

    /// <summary>
    /// Obtém o erro da falha.
    /// </summary>
    /// <exception cref="InvalidOperationException">O resultado é de sucesso.</exception>
    public Erro Erro => _erro ?? throw new InvalidOperationException("Um resultado de sucesso não tem erro.");
}
