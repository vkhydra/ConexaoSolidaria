namespace Notificacoes.Function.Recibos;

/// <summary>
/// A mensagem da fila não é um envelope do MassTransit legível: JSON malformado ou sem o campo <c>message</c> esperado.
/// Não adianta repetir; a mensagem segue para a fila <c>notificacoes-recibo_error</c>.
/// </summary>
public sealed class EnvelopeInvalidoException : Exception
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public EnvelopeInvalidoException()
        : base("O envelope do MassTransit é inválido.")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem da exceção.</param>
    public EnvelopeInvalidoException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a causa.
    /// </summary>
    /// <param name="message">Mensagem da exceção.</param>
    /// <param name="innerException">Falha de leitura que originou esta exceção.</param>
    public EnvelopeInvalidoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
