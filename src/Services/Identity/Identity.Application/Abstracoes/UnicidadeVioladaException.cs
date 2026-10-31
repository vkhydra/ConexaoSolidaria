namespace Identity.Application.Abstracoes;

/// <summary>
/// Campo cuja unicidade o banco garante.
/// </summary>
public enum CampoUnico
{
    /// <summary>
    /// O e-mail do usuário.
    /// </summary>
    Email,

    /// <summary>
    /// O CPF do doador.
    /// </summary>
    Cpf,
}

/// <summary>
/// Lançada pelo repositório quando duas gravações simultâneas disputam o mesmo e-mail ou CPF e o banco recusa a segunda.
/// O caso de uso a converte no erro de negócio de conflito (409).
/// </summary>
public sealed class UnicidadeVioladaException : Exception
{
    /// <summary>
    /// Inicializa a exceção sem detalhes.
    /// </summary>
    public UnicidadeVioladaException()
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    public UnicidadeVioladaException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem sem dados pessoais.</param>
    /// <param name="innerException">Exceção original do banco.</param>
    public UnicidadeVioladaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Inicializa a exceção indicando o campo em conflito.
    /// </summary>
    /// <param name="campo">Campo que violou a unicidade.</param>
    /// <param name="innerException">Exceção original do banco.</param>
    public UnicidadeVioladaException(CampoUnico campo, Exception innerException)
        : base($"Violação de unicidade em {campo}.", innerException)
    {
        Campo = campo;
    }

    /// <summary>
    /// Obtém o campo que violou a unicidade.
    /// </summary>
    public CampoUnico Campo { get; }
}
