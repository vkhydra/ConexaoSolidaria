using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;

namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Calcula o HMAC-SHA256 do e-mail normalizado, com a chave <c>Autenticacao__ChaveHashEmail</c>. O hash identifica
/// o controle de tentativas de login sem guardar o e-mail em texto.
/// </summary>
public sealed class HasherEmailHmac : IHasherEmail
{
    /// <summary>
    /// Chave secreta do HMAC.
    /// </summary>
    private readonly byte[] _chave;

    /// <summary>
    /// Cria o hasher com a chave secreta.
    /// </summary>
    /// <param name="chave">Chave do HMAC, vinda de Secret; nunca é registrada em log.</param>
    /// <exception cref="ArgumentException">A chave está ausente.</exception>
    public HasherEmailHmac(string chave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);

        _chave = Encoding.UTF8.GetBytes(chave);
    }

    /// <inheritdoc/>
    public byte[] CalcularHash(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return HMACSHA256.HashData(_chave, Encoding.UTF8.GetBytes(email.Valor));
    }
}
