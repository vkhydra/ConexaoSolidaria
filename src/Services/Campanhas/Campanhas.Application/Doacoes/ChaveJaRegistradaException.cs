namespace Campanhas.Application.Doacoes;

/// <summary>
/// Lançada pelo publicador quando duas requisições simultâneas registram a mesma <c>Idempotency-Key</c> e o banco recusa a
/// segunda. O caso de uso relê a chave e devolve a doação da primeira.
/// </summary>
public sealed class ChaveJaRegistradaException : Exception
{
    /// <summary>
    /// Inicializa a exceção sem detalhes.
    /// </summary>
    public ChaveJaRegistradaException()
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    public ChaveJaRegistradaException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    /// <param name="innerException">Exceção original do banco.</param>
    public ChaveJaRegistradaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
