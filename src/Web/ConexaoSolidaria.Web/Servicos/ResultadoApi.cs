namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Erro devolvido por uma API, já traduzido do <c>application/problem+json</c>.
/// </summary>
/// <param name="Status">Status HTTP; 503 quando a API nem respondeu.</param>
/// <param name="Codigo">Código estável do erro (por exemplo, <c>auth.credenciais_invalidas</c>); nulo se a API não informou.</param>
/// <param name="Mensagem">Título do problema, em português.</param>
/// <param name="Campos">Mensagens de validação por campo; vazio quando não há.</param>
/// <param name="TraceId">Identificador de rastreio, para o usuário informar à equipe; nulo se não veio.</param>
public sealed record ErroApi(int Status, string? Codigo, string Mensagem, IReadOnlyDictionary<string, string[]> Campos, string? TraceId);

/// <summary>
/// Resultado de uma chamada à API sem valor de retorno: sucesso ou <see cref="ErroApi"/>.
/// </summary>
public class ResultadoApi
{
    /// <summary>
    /// Inicializa o resultado.
    /// </summary>
    /// <param name="erro">Erro da falha, ou nulo para sucesso.</param>
    protected ResultadoApi(ErroApi? erro)
    {
        Erro = erro;
    }

    /// <summary>
    /// Obtém um valor que indica se a chamada deu certo.
    /// </summary>
    public bool Sucesso => Erro is null;

    /// <summary>
    /// Obtém o erro da falha; nulo quando houve sucesso.
    /// </summary>
    public ErroApi? Erro { get; }

    /// <summary>
    /// Cria um resultado de sucesso sem valor.
    /// </summary>
    /// <returns>O resultado de sucesso.</returns>
    public static ResultadoApi Ok()
    {
        return new ResultadoApi(null);
    }

    /// <summary>
    /// Cria um resultado de falha.
    /// </summary>
    /// <param name="erro">Erro da API.</param>
    /// <returns>O resultado de falha.</returns>
    public static ResultadoApi Falha(ErroApi erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return new ResultadoApi(erro);
    }

    /// <summary>
    /// Cria um resultado de sucesso com valor.
    /// </summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="valor">Valor devolvido pela API.</param>
    /// <returns>O resultado de sucesso.</returns>
    public static ResultadoApi<T> Ok<T>(T valor)
    {
        return new ResultadoApi<T>(valor, null);
    }

    /// <summary>
    /// Cria um resultado de falha com tipo de valor.
    /// </summary>
    /// <typeparam name="T">Tipo do valor que a chamada produziria.</typeparam>
    /// <param name="erro">Erro da API.</param>
    /// <returns>O resultado de falha.</returns>
    public static ResultadoApi<T> Falha<T>(ErroApi erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        return new ResultadoApi<T>(default, erro);
    }
}

/// <summary>
/// Resultado de uma chamada à API com valor de retorno: sucesso com o valor ou <see cref="ErroApi"/>.
/// </summary>
/// <typeparam name="T">Tipo do valor.</typeparam>
public sealed class ResultadoApi<T> : ResultadoApi
{
    /// <summary>
    /// Valor devolvido pela API.
    /// </summary>
    private readonly T? _valor;

    /// <summary>
    /// Inicializa o resultado.
    /// </summary>
    /// <param name="valor">Valor do sucesso.</param>
    /// <param name="erro">Erro da falha, ou nulo para sucesso.</param>
    internal ResultadoApi(T? valor, ErroApi? erro)
        : base(erro)
    {
        _valor = valor;
    }

    /// <summary>
    /// Obtém o valor devolvido pela API.
    /// </summary>
    /// <exception cref="InvalidOperationException">O resultado é uma falha.</exception>
    public T Valor => Sucesso ? _valor! : throw new InvalidOperationException("Uma falha não tem valor.");
}
