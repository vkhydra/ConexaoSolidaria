using Identity.Domain.Usuarios;

namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta que emite o token de acesso de um usuário autenticado. A implementação assina um JWT RS256.
/// </summary>
public interface IEmissorToken
{
    /// <summary>
    /// Emite o token de acesso. O token leva só o identificador e o papel, nunca dados pessoais.
    /// </summary>
    /// <param name="usuario">Usuário autenticado.</param>
    /// <returns>O token e o tempo de validade.</returns>
    TokenEmitido Emitir(Usuario usuario);
}

/// <summary>
/// Token de acesso emitido.
/// </summary>
/// <param name="AccessToken">JWT assinado.</param>
/// <param name="ExpiraEmSegundos">Validade do token, em segundos.</param>
public sealed record TokenEmitido(string AccessToken, int ExpiraEmSegundos);
