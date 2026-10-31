using Identity.Application.Abstracoes;
using Identity.Domain.Comum;
using Identity.Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Titular;

/// <summary>
/// Direito de eliminação (LGPD): anonimiza o cadastro do doador em uma única gravação, então ou tudo muda ou nada muda.
/// As doações continuam ligadas só ao identificador, que depois disso não identifica mais ninguém.
/// </summary>
/// <param name="usuarios">Repositório de usuários.</param>
/// <param name="relogio">Relógio da aplicação.</param>
/// <param name="metricas">Métricas de doadores.</param>
/// <param name="log">Logger de auditoria; recebe só o identificador, a ação e a data.</param>
public sealed partial class AnonimizarMeusDados(
    IUsuarioRepositorio usuarios,
    TimeProvider relogio,
    IMetricasDoadores metricas,
    ILogger<AnonimizarMeusDados> log)
{
    /// <summary>
    /// Executa a anonimização.
    /// </summary>
    /// <param name="usuarioId">Identificador vindo da claim <c>sub</c> do token.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Sucesso, ou <c>auth.nao_autenticado</c> se o usuário não existe ou já foi anonimizado.</returns>
    public async Task<Result> ExecutarAsync(UsuarioId usuarioId, CancellationToken cancelamento)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioId, cancelamento);
        if (usuario is null || usuario.EstaAnonimizado)
        {
            return Result.Falha(ErrosDoTitular.NaoAutenticado);
        }

        var agora = relogio.GetUtcNow();
        usuario.Anonimizar(agora);
        await usuarios.SalvarAsync(usuario, cancelamento);

        metricas.RegistrarAnonimizacao();
        LogTitularAnonimizado(log, usuario.Id, agora);
        return Result.Ok();
    }

    /// <summary>
    /// Registra a auditoria da anonimização: ação, identificador e data, sem nenhum dado pessoal.
    /// </summary>
    /// <param name="log">Logger de auditoria.</param>
    /// <param name="usuarioId">Identificador do titular.</param>
    /// <param name="em">Data e hora da anonimização.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "TitularAnonimizado: {UsuarioId} em {Em:o}.")]
    private static partial void LogTitularAnonimizado(ILogger log, UsuarioId usuarioId, DateTimeOffset em);
}
