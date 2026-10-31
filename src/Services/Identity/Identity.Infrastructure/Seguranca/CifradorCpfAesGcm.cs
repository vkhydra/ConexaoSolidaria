using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;

namespace Identity.Infrastructure.Seguranca;

/// <summary>
/// Protege o CPF com AES-256-GCM (cifra) e HMAC-SHA256 (hash de unicidade). Formato do cifrado:
/// <c>| 1 byte versão da chave | 12 bytes nonce | 16 bytes tag | N bytes cifra |</c>. A versão entra como dado
/// autenticado adicional, então trocá-la invalida a tag.
/// </summary>
public sealed class CifradorCpfAesGcm : ICifradorCpf
{
    /// <summary>
    /// Tamanho da chave em bytes (256 bits).
    /// </summary>
    private const int _tamanhoChave = 32;

    /// <summary>
    /// Tamanho do nonce do AES-GCM.
    /// </summary>
    private const int _tamanhoNonce = 12;

    /// <summary>
    /// Tamanho da tag de autenticação do AES-GCM.
    /// </summary>
    private const int _tamanhoTag = 16;

    /// <summary>
    /// Chave de cifra.
    /// </summary>
    private readonly byte[] _chaveCifra;

    /// <summary>
    /// Chave do HMAC.
    /// </summary>
    private readonly byte[] _chaveHash;

    /// <summary>
    /// Versão da chave de cifra.
    /// </summary>
    private readonly byte _versao;

    /// <summary>
    /// Cria o cifrador a partir das chaves configuradas.
    /// </summary>
    /// <param name="opcoes">Chaves em Base64 e versão da chave de cifra.</param>
    /// <exception cref="ArgumentException">Uma das chaves está ausente, não é Base64 ou não tem 32 bytes.</exception>
    public CifradorCpfAesGcm(CpfOpcoes opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        _chaveCifra = LerChave(opcoes.ChaveCifra, nameof(opcoes.ChaveCifra));
        _chaveHash = LerChave(opcoes.ChaveHash, nameof(opcoes.ChaveHash));
        _versao = opcoes.VersaoChave;
    }

    /// <inheritdoc/>
    public byte[] Cifrar(Cpf cpf)
    {
        ArgumentNullException.ThrowIfNull(cpf);

        var texto = Encoding.UTF8.GetBytes(cpf.Valor);
        var resultado = new byte[1 + _tamanhoNonce + _tamanhoTag + texto.Length];
        resultado[0] = _versao;
        var nonce = resultado.AsSpan(1, _tamanhoNonce);
        var tag = resultado.AsSpan(1 + _tamanhoNonce, _tamanhoTag);
        var cifra = resultado.AsSpan(1 + _tamanhoNonce + _tamanhoTag);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_chaveCifra, _tamanhoTag);
        aes.Encrypt(nonce, texto, cifra, tag, resultado.AsSpan(0, 1));
        return resultado;
    }

    /// <inheritdoc/>
    /// <exception cref="CryptographicException">O formato, a versão da chave ou a tag de autenticação não conferem.</exception>
    public Cpf Decifrar(byte[] cifrado)
    {
        ArgumentNullException.ThrowIfNull(cifrado);

        if (cifrado.Length <= 1 + _tamanhoNonce + _tamanhoTag || cifrado[0] != _versao)
        {
            throw new CryptographicException("O CPF cifrado está em formato ou versão de chave desconhecidos.");
        }

        var nonce = cifrado.AsSpan(1, _tamanhoNonce);
        var tag = cifrado.AsSpan(1 + _tamanhoNonce, _tamanhoTag);
        var cifra = cifrado.AsSpan(1 + _tamanhoNonce + _tamanhoTag);
        var texto = new byte[cifra.Length];

        using var aes = new AesGcm(_chaveCifra, _tamanhoTag);
        aes.Decrypt(nonce, cifra, tag, texto, cifrado.AsSpan(0, 1));

        var resultado = Cpf.Criar(Encoding.UTF8.GetString(texto));
        return resultado.EhSucesso
            ? resultado.Valor
            : throw new CryptographicException("O conteúdo decifrado não é um CPF válido.");
    }

    /// <inheritdoc/>
    public byte[] CalcularHash(Cpf cpf)
    {
        ArgumentNullException.ThrowIfNull(cpf);

        return HMACSHA256.HashData(_chaveHash, Encoding.UTF8.GetBytes(cpf.Valor));
    }

    /// <summary>
    /// Lê uma chave Base64 de 32 bytes.
    /// </summary>
    /// <param name="base64">Chave em Base64.</param>
    /// <param name="nome">Nome da opção, para a mensagem de erro (sem o valor).</param>
    /// <returns>A chave em bytes.</returns>
    private static byte[] LerChave(string base64, string nome)
    {
        byte[] chave;
        try
        {
            chave = Convert.FromBase64String(base64 ?? string.Empty);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Cpf:{nome} não é Base64 válido.", nameof(base64));
        }

        return chave.Length == _tamanhoChave
            ? chave
            : throw new ArgumentException($"Cpf:{nome} deve ter 32 bytes (256 bits) em Base64.", nameof(base64));
    }
}
