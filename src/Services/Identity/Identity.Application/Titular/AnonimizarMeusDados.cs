using Identity.Application.Abstracoes;
using Identity.Domain.Comum;
using Identity.Domain.Usuarios;

namespace Identity.Application.Titular;

/// <summary>
/// Direito de eliminação (LGPD): anonimiza o cadastro do doador em uma única gravação, então ou tudo muda ou nada muda.
/// As doações continuam ligadas só ao identificador, que depois disso não identifica mais ninguém.
/// </summary>
/// <param name="usuarios">Repositório de usuários.</param>
/// <param name="relogio">Relógio da aplicação.</param>
public sealed class AnonimizarMeusDados(IUsuarioRepositorio usuarios, TimeProvider relogio)
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

        usuario.Anonimizar(relogio.GetUtcNow());
        await usuarios.SalvarAsync(usuario, cancelamento);
        return Result.Ok();
    }
}
