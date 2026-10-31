namespace Campanhas.Application.Busca;

/// <summary>
/// O mecanismo de busca (Elasticsearch) está fora do ar ou não respondeu a tempo. Lançada pela infraestrutura e
/// convertida pelo caso de uso no erro <c>busca.indisponivel</c>.
/// </summary>
public sealed class BuscaIndisponivelException : Exception
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public BuscaIndisponivelException()
        : base("O mecanismo de busca está indisponível.")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem da exceção.</param>
    public BuscaIndisponivelException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem da exceção.</param>
    /// <param name="innerException">Falha de transporte que originou esta exceção.</param>
    public BuscaIndisponivelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
