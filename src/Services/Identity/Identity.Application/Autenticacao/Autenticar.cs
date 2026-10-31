using FluentValidation;
using Identity.Application.Abstracoes;
using Identity.Domain.Autenticacao;
using Identity.Domain.Comum;
using Identity.Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Autenticacao;

/// <summary>
/// Caso de uso do login: valida a entrada, confere o bloqueio por tentativas, verifica a senha em tempo
/// constante e emite o token. A mesma resposta vale para e-mail inexistente, senha errada e usuário anonimizado.
/// </summary>
/// <param name="usuarios">Repositório de usuários.</param>
/// <param name="tentativas">Repositório do controle de tentativas.</param>
/// <param name="hasherSenha">Verificador de senha.</param>
/// <param name="hasherEmail">Calculador do hash do e-mail.</param>
/// <param name="emissor">Emissor do token de acesso.</param>
/// <param name="validador">Validador do pedido de login.</param>
/// <param name="relogio">Relógio da aplicação.</param>
/// <param name="metricas">Métrica de logins.</param>
/// <param name="log">Logger; nunca recebe e-mail nem senha.</param>
public sealed partial class Autenticar(
    IUsuarioRepositorio usuarios,
    ITentativaLoginRepositorio tentativas,
    IHasherSenha hasherSenha,
    IHasherEmail hasherEmail,
    IEmissorToken emissor,
    IValidator<AutenticarRequest> validador,
    TimeProvider relogio,
    IMetricasLogin metricas,
    ILogger<Autenticar> log)
{
    /// <summary>
    /// Código do erro de entrada inválida.
    /// </summary>
    public const string CodigoValidacao = "validacao";

    /// <summary>
    /// Código do erro de credenciais inválidas.
    /// </summary>
    public const string CodigoCredenciaisInvalidas = "auth.credenciais_invalidas";

    /// <summary>
    /// Código do erro de login bloqueado por excesso de falhas.
    /// </summary>
    public const string CodigoBloqueado = "auth.bloqueado";

    /// <summary>
    /// Tipo do token devolvido na resposta.
    /// </summary>
    public const string TipoDoToken = "Bearer";

    /// <summary>
    /// Executa o login.
    /// </summary>
    /// <param name="request">Dados do login.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O token emitido ou um erro: <c>validacao</c>, <c>auth.credenciais_invalidas</c> ou <c>auth.bloqueado</c>.</returns>
    public async Task<Result<AutenticarResponse>> ExecutarAsync(AutenticarRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            var campos = validacao.Errors
                .GroupBy(e => e.PropertyName.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Falha<AutenticarResponse>(new Erro(CodigoValidacao, "Dados inválidos.") { Campos = campos });
        }

        var email = Email.Criar(request.Email).Valor;
        var agora = relogio.GetUtcNow();
        var emailHash = hasherEmail.CalcularHash(email);
        var tentativa = await tentativas.ObterAsync(emailHash, cancelamento);

        if (tentativa is not null && tentativa.EstaBloqueado(agora))
        {
            metricas.Registrar(ResultadoLogin.Bloqueado);
            LogBloqueado(log);
            return Result.Falha<AutenticarResponse>(new Erro(CodigoBloqueado, "Muitas tentativas. Tente novamente mais tarde."));
        }

        var usuario = await usuarios.ObterPorEmailAsync(email, cancelamento);
        var senhaCorreta = ConferirSenha(usuario, request.Senha!);

        if (!senhaCorreta)
        {
            tentativa ??= TentativaLogin.Nova(emailHash, agora);
            tentativa.RegistrarFalha(agora);
            await tentativas.SalvarAsync(tentativa, cancelamento);
            metricas.Registrar(ResultadoLogin.Falha);
            LogFalha(log, tentativa.Falhas);
            return Result.Falha<AutenticarResponse>(new Erro(CodigoCredenciaisInvalidas, "E-mail ou senha inválidos"));
        }

        if (tentativa is not null)
        {
            tentativa.RegistrarSucesso(agora);
            await tentativas.SalvarAsync(tentativa, cancelamento);
        }

        var token = emissor.Emitir(usuario!);
        metricas.Registrar(ResultadoLogin.Sucesso);
        LogSucesso(log, usuario!.Id);
        return Result.Sucesso(new AutenticarResponse(token.AccessToken, TipoDoToken, token.ExpiraEmSegundos, usuario!.Role.ToString()));
    }

    /// <summary>
    /// Confere a senha gastando o mesmo trabalho de hash quando o usuário não existe ou foi anonimizado.
    /// </summary>
    /// <param name="usuario">Usuário encontrado ou nulo.</param>
    /// <param name="senha">Senha digitada.</param>
    /// <returns><c>true</c> se a senha confere com um cadastro ativo.</returns>
    private bool ConferirSenha(Usuario? usuario, string senha)
    {
        if (usuario is null || usuario.EstaAnonimizado)
        {
            hasherSenha.VerificarSemUsuario(senha);
            return false;
        }

        return usuario.VerificarSenha(senha, hasherSenha);
    }

    /// <summary>
    /// Registra um login bem-sucedido, só com o identificador do usuário.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="usuarioId">Identificador do usuário autenticado.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Login realizado: {UsuarioId}.")]
    private static partial void LogSucesso(ILogger log, UsuarioId usuarioId);

    /// <summary>
    /// Registra uma falha de login, sem e-mail nem senha.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="falhas">Falhas acumuladas na janela atual.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha de login ({Falhas} na janela atual).")]
    private static partial void LogFalha(ILogger log, int falhas);

    /// <summary>
    /// Registra uma tentativa recusada por bloqueio, sem e-mail.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de login recusada: e-mail bloqueado por excesso de falhas.")]
    private static partial void LogBloqueado(ILogger log);
}
