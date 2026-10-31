namespace Identity.Domain.Autenticacao;

/// <summary>
/// Controle de tentativas de login de um e-mail, para barrar força bruta: 5 falhas em 15 minutos
/// bloqueiam o e-mail por 15 minutos. O e-mail nunca é guardado em texto, só o hash dele.
/// </summary>
public sealed class TentativaLogin
{
    /// <summary>
    /// Quantidade de falhas dentro da janela que provoca o bloqueio.
    /// </summary>
    public const int LimiteDeFalhas = 5;

    /// <summary>
    /// Duração da janela em que as falhas são contadas.
    /// </summary>
    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Duração do bloqueio depois de atingido o limite de falhas.
    /// </summary>
    public static readonly TimeSpan Bloqueio = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Construtor sem parâmetros usado pelo EF Core para materializar a entidade.
    /// </summary>
    private TentativaLogin()
    {
        EmailHash = [];
    }

    /// <summary>
    /// Cria o controle de um e-mail, sem falhas.
    /// </summary>
    /// <param name="emailHash">Hash (HMAC) do e-mail normalizado.</param>
    /// <param name="agora">Data e hora atuais, que abrem a primeira janela.</param>
    private TentativaLogin(byte[] emailHash, DateTimeOffset agora)
    {
        EmailHash = emailHash;
        JanelaInicio = agora;
    }

    /// <summary>
    /// Obtém o hash do e-mail, que identifica o controle.
    /// </summary>
    public byte[] EmailHash { get; private set; }

    /// <summary>
    /// Obtém a quantidade de falhas dentro da janela atual.
    /// </summary>
    public int Falhas { get; private set; }

    /// <summary>
    /// Obtém o início da janela de contagem de falhas.
    /// </summary>
    public DateTimeOffset JanelaInicio { get; private set; }

    /// <summary>
    /// Obtém o fim do bloqueio; nulo quando o e-mail não está bloqueado.
    /// </summary>
    public DateTimeOffset? BloqueadoAte { get; private set; }

    /// <summary>
    /// Cria o controle de tentativas de um e-mail que ainda não falhou.
    /// </summary>
    /// <param name="emailHash">Hash (HMAC) do e-mail normalizado.</param>
    /// <param name="agora">Data e hora atuais.</param>
    /// <returns>O controle sem falhas.</returns>
    public static TentativaLogin Nova(byte[] emailHash, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(emailHash);

        return new TentativaLogin(emailHash, agora);
    }

    /// <summary>
    /// Indica se o e-mail está bloqueado no instante informado.
    /// </summary>
    /// <param name="agora">Data e hora atuais.</param>
    /// <returns><c>true</c> enquanto o fim do bloqueio não chegou.</returns>
    public bool EstaBloqueado(DateTimeOffset agora)
    {
        return BloqueadoAte is not null && BloqueadoAte > agora;
    }

    /// <summary>
    /// Registra uma falha de login. A janela é reiniciada se expirou ou se o bloqueio anterior terminou;
    /// na falha de número <see cref="LimiteDeFalhas"/> dentro da janela, o e-mail é bloqueado.
    /// </summary>
    /// <param name="agora">Data e hora da falha.</param>
    public void RegistrarFalha(DateTimeOffset agora)
    {
        if (agora - JanelaInicio > Janela || (BloqueadoAte is not null && BloqueadoAte <= agora))
        {
            Falhas = 0;
            JanelaInicio = agora;
            BloqueadoAte = null;
        }

        Falhas++;
        if (Falhas >= LimiteDeFalhas)
        {
            BloqueadoAte = agora + Bloqueio;
        }
    }

    /// <summary>
    /// Registra um login com sucesso, zerando a contagem de falhas e o bloqueio.
    /// </summary>
    /// <param name="agora">Data e hora do login.</param>
    public void RegistrarSucesso(DateTimeOffset agora)
    {
        Falhas = 0;
        JanelaInicio = agora;
        BloqueadoAte = null;
    }
}
