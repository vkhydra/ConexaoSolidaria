using Identity.Domain.Usuarios;

namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta que calcula o hash (HMAC) de um e-mail, para identificar o controle de tentativas de login
/// sem guardar o e-mail em texto.
/// </summary>
public interface IHasherEmail
{
    /// <summary>
    /// Calcula o HMAC-SHA256 do e-mail normalizado com a chave secreta do serviço.
    /// </summary>
    /// <param name="email">E-mail normalizado.</param>
    /// <returns>Hash de 32 bytes.</returns>
    byte[] CalcularHash(Email email);
}
