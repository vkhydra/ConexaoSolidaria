using Identity.Domain.Comum;

namespace Identity.Domain.Usuarios;

/// <summary>
/// Senha em texto que cumpre a política de segurança: 8 a 64 caracteres, com letra, número e caractere especial.
/// Existe só até o hash: nunca é guardada, e <see cref="ToString"/> não a revela.
/// </summary>
public sealed record Senha
{
    /// <summary>
    /// Código do erro de senha fraca.
    /// </summary>
    public const string CodigoFraca = "senha.fraca";

    /// <summary>
    /// Mensagem de senha fraca, que explica a política sem repetir a senha.
    /// </summary>
    public const string MensagemFraca = "A senha deve ter de 8 a 64 caracteres, com letra, número e caractere especial.";

    /// <summary>
    /// Tamanho mínimo da senha.
    /// </summary>
    public const int TamanhoMinimo = 8;

    /// <summary>
    /// Tamanho máximo da senha, abaixo do limite de 72 bytes do BCrypt para evitar truncamento silencioso.
    /// </summary>
    public const int TamanhoMaximo = 64;

    /// <summary>
    /// Cria a senha já validada.
    /// </summary>
    /// <param name="valor">Senha em texto.</param>
    private Senha(string valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Obtém a senha em texto, para entregar ao gerador de hash. Nunca deve ir para log.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Valida a senha contra a política de segurança.
    /// </summary>
    /// <param name="texto">Senha digitada.</param>
    /// <returns>A senha validada ou o erro <c>senha.fraca</c>.</returns>
    public static Result<Senha> Criar(string? texto)
    {
        if (texto is null
            || texto.Length is < TamanhoMinimo or > TamanhoMaximo
            || !texto.Any(char.IsLetter)
            || !texto.Any(char.IsDigit)
            || !texto.Any(c => !char.IsLetterOrDigit(c)))
        {
            return Result.Falha<Senha>(new Erro(CodigoFraca, MensagemFraca));
        }

        return Result.Sucesso(new Senha(texto));
    }

    /// <summary>
    /// Devolve um texto fixo, para que a senha nunca vá parar em log por acidente.
    /// </summary>
    /// <returns>O texto <c>***</c>.</returns>
    public override string ToString()
    {
        return "***";
    }
}
