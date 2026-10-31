namespace Arrecadacao.Worker.Consumidores;

/// <summary>
/// Falha permanente no processamento de uma doação (valor inválido, campanha inexistente): repetir não adianta, então a
/// mensagem vai direto para a fila <c>doacao-recebida_error</c>, sem retentativas, e o alerta de fila de erro avisa a operação.
/// </summary>
public sealed class DoacaoNaoProcessavelException : Exception
{
    /// <summary>
    /// Inicializa a exceção sem detalhes.
    /// </summary>
    public DoacaoNaoProcessavelException()
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem com o código do erro de negócio, sem dados pessoais.</param>
    public DoacaoNaoProcessavelException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem com o código do erro de negócio, sem dados pessoais.</param>
    /// <param name="innerException">Causa original.</param>
    public DoacaoNaoProcessavelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
