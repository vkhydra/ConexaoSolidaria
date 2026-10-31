using Identity.Domain.Comum;

namespace Identity.Domain.Usuarios;

/// <summary>
/// Aceite da política de privacidade (LGPD): qual versão do texto o doador aceitou e quando.
/// </summary>
public sealed record Consentimento
{
    /// <summary>
    /// Código do erro de aceite ausente.
    /// </summary>
    public const string CodigoObrigatorio = "consentimento.obrigatorio";

    /// <summary>
    /// Tamanho máximo da versão da política, igual ao da coluna do banco.
    /// </summary>
    public const int TamanhoMaximoVersao = 20;

    /// <summary>
    /// Cria o consentimento já validado.
    /// </summary>
    /// <param name="versao">Versão da política aceita.</param>
    /// <param name="em">Data e hora do aceite.</param>
    private Consentimento(string versao, DateTimeOffset em)
    {
        Versao = versao;
        Em = em;
    }

    /// <summary>
    /// Obtém a versão da política de privacidade aceita (por exemplo, <c>2026-10</c>).
    /// </summary>
    public string Versao { get; }

    /// <summary>
    /// Obtém a data e a hora do aceite.
    /// </summary>
    public DateTimeOffset Em { get; }

    /// <summary>
    /// Registra o aceite. Sem aceite, sem versão ou com versão maior que a coluna, o cadastro não pode seguir.
    /// </summary>
    /// <param name="aceitou">Se o visitante marcou o aceite da política.</param>
    /// <param name="versao">Versão da política exibida no formulário.</param>
    /// <param name="agora">Data e hora atuais.</param>
    /// <returns>O consentimento ou o erro <c>consentimento.obrigatorio</c>.</returns>
    public static Result<Consentimento> Criar(bool aceitou, string? versao, DateTimeOffset agora)
    {
        if (!aceitou || string.IsNullOrWhiteSpace(versao) || versao.Length > TamanhoMaximoVersao)
        {
            return Result.Falha<Consentimento>(new Erro(CodigoObrigatorio, "É obrigatório aceitar a política de privacidade."));
        }

        return Result.Sucesso(new Consentimento(versao, agora));
    }
}
