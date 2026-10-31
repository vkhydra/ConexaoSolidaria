using Identity.Application.Abstracoes;
using Identity.Domain.Comum;
using Identity.Domain.Usuarios;

namespace Identity.Application.Titular;

/// <summary>
/// Direito de acesso (LGPD): devolve ao doador os dados que a plataforma guarda sobre ele, com o CPF mascarado.
/// Usuário inexistente ou anonimizado é tratado como não autenticado, o que invalida tokens antigos.
/// </summary>
/// <param name="usuarios">Repositório de usuários.</param>
public sealed class ObterMeusDados(IUsuarioRepositorio usuarios)
{
    /// <summary>
    /// Executa a consulta.
    /// </summary>
    /// <param name="usuarioId">Identificador vindo da claim <c>sub</c> do token.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os dados do doador ou <c>auth.nao_autenticado</c>.</returns>
    public async Task<Result<DoadorResponse>> ExecutarAsync(UsuarioId usuarioId, CancellationToken cancelamento)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioId, cancelamento);
        if (usuario is null || usuario.EstaAnonimizado || usuario.Consentimento is null)
        {
            return Result.Falha<DoadorResponse>(ErrosDoTitular.NaoAutenticado);
        }

        var cpf = await usuarios.ObterCpfAsync(usuarioId, cancelamento);
        if (cpf is null)
        {
            return Result.Falha<DoadorResponse>(ErrosDoTitular.NaoAutenticado);
        }

        return Result.Sucesso(new DoadorResponse(
            usuario.Id.Valor,
            usuario.NomeCompleto,
            usuario.Email.Valor,
            cpf.Mascarado(),
            usuario.CriadoEm,
            new ConsentimentoResponse(usuario.Consentimento.Versao, usuario.Consentimento.Em)));
    }
}
