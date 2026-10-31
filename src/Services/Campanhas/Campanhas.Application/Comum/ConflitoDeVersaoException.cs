namespace Campanhas.Application.Comum;

/// <summary>
/// Lançada pelo repositório quando outra gravação alterou a campanha entre a leitura e a gravação (concorrência otimista).
/// O caso de uso a converte no erro de negócio <c>campanha.conflito_de_versao</c> (409).
/// </summary>
public sealed class ConflitoDeVersaoException : Exception
{
    /// <summary>
    /// Inicializa a exceção sem detalhes.
    /// </summary>
    public ConflitoDeVersaoException()
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    public ConflitoDeVersaoException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    /// <param name="innerException">Exceção original do banco.</param>
    public ConflitoDeVersaoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
