using System.Security.Cryptography;
using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Emite o JWT de acesso assinado com RS256. O payload leva só <c>sub</c>, <c>role</c>, <c>jti</c>, <c>iat</c>,
/// <c>exp</c>, <c>iss</c> e <c>aud</c>: nunca nome, e-mail ou CPF.
/// </summary>
public sealed class EmissorJwtRs256 : IEmissorToken, IDisposable
{
    /// <summary>
    /// Opções do token.
    /// </summary>
    private readonly JwtOpcoes _opcoes;

    /// <summary>
    /// Relógio da aplicação.
    /// </summary>
    private readonly TimeProvider _relogio;

    /// <summary>
    /// Chave RSA carregada da configuração.
    /// </summary>
    private readonly RSA _rsa;

    /// <summary>
    /// Credenciais de assinatura RS256.
    /// </summary>
    private readonly SigningCredentials _credenciais;

    /// <summary>
    /// Manipulador que monta e assina o JWT.
    /// </summary>
    private readonly JsonWebTokenHandler _manipulador = new() { SetDefaultTimesOnTokenCreation = false };

    /// <summary>
    /// Carrega a chave privada e prepara a assinatura.
    /// </summary>
    /// <param name="opcoes">Opções do token, com a chave privada em PEM.</param>
    /// <param name="relogio">Relógio da aplicação.</param>
    /// <exception cref="ArgumentException">A chave privada está ausente ou não é um PEM válido.</exception>
    public EmissorJwtRs256(JwtOpcoes opcoes, TimeProvider relogio)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        ArgumentNullException.ThrowIfNull(relogio);

        if (string.IsNullOrWhiteSpace(opcoes.ChavePrivadaPem))
        {
            throw new ArgumentException("A chave privada do JWT não foi configurada.", nameof(opcoes));
        }

        _opcoes = opcoes;
        _relogio = relogio;
        _rsa = RSA.Create();
        try
        {
            _rsa.ImportFromPem(opcoes.ChavePrivadaPem);
        }
        catch (Exception excecao) when (excecao is CryptographicException or ArgumentException)
        {
            _rsa.Dispose();
            throw new ArgumentException("A chave privada do JWT não é um PEM RSA válido.", nameof(opcoes));
        }

        _credenciais = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);
    }

    /// <inheritdoc/>
    public TokenEmitido Emitir(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var agora = _relogio.GetUtcNow();
        var descritor = new SecurityTokenDescriptor
        {
            SigningCredentials = _credenciais,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Iss] = _opcoes.Issuer,
                [JwtRegisteredClaimNames.Aud] = _opcoes.Audience,
                [JwtRegisteredClaimNames.Iat] = agora.ToUnixTimeSeconds(),
                [JwtRegisteredClaimNames.Exp] = agora.AddSeconds(_opcoes.ValidadeEmSegundos).ToUnixTimeSeconds(),
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
                ["role"] = usuario.Role.ToString(),
            },
        };

        return new TokenEmitido(_manipulador.CreateToken(descritor), _opcoes.ValidadeEmSegundos);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _rsa.Dispose();
    }
}
