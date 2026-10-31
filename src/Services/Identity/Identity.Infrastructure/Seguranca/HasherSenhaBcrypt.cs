using Identity.Domain.Usuarios;

namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Hash de senha com BCrypt (work factor 12). A verificação de um usuário inexistente gasta o mesmo trabalho
/// de uma verificação real, para que o tempo de resposta não revele se o e-mail existe.
/// </summary>
public sealed class HasherSenhaBcrypt : IHasherSenha
{
    /// <summary>
    /// Custo do BCrypt: 2^12 iterações.
    /// </summary>
    public const int WorkFactor = 12;

    /// <summary>
    /// Hash fictício, com o mesmo custo dos hashes reais, usado quando o usuário não existe.
    /// </summary>
    private static readonly string _hashFicticio = BCrypt.Net.BCrypt.HashPassword("senha-ficticia-sem-uso", WorkFactor);

    /// <inheritdoc/>
    public string Hash(string senha)
    {
        ArgumentException.ThrowIfNullOrEmpty(senha);

        return BCrypt.Net.BCrypt.HashPassword(senha, WorkFactor);
    }

    /// <inheritdoc/>
    public bool Verificar(string senha, string hash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool VerificarSemUsuario(string senha)
    {
        _ = BCrypt.Net.BCrypt.Verify(senha ?? string.Empty, _hashFicticio);
        return false;
    }
}
