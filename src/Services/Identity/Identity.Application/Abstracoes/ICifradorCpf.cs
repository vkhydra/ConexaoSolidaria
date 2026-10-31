using Identity.Domain.Usuarios;

namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta que protege o CPF: cifra para guardar (AES-256-GCM) e calcula o hash (HMAC-SHA256) para conferir unicidade.
/// </summary>
public interface ICifradorCpf
{
    /// <summary>
    /// Cifra o CPF. O resultado leva a versão da chave, o nonce, a tag e o texto cifrado.
    /// </summary>
    /// <param name="cpf">CPF a proteger.</param>
    /// <returns>Os bytes cifrados, prontos para a coluna <c>cpf_cifrado</c>.</returns>
    byte[] Cifrar(Cpf cpf);

    /// <summary>
    /// Decifra um CPF guardado.
    /// </summary>
    /// <param name="cifrado">Bytes lidos da coluna <c>cpf_cifrado</c>.</param>
    /// <returns>O CPF original.</returns>
    Cpf Decifrar(byte[] cifrado);

    /// <summary>
    /// Calcula o HMAC-SHA256 do CPF, que serve para saber se ele já está cadastrado sem decifrar nada.
    /// </summary>
    /// <param name="cpf">CPF a conferir.</param>
    /// <returns>Hash de 32 bytes, igual para o mesmo CPF.</returns>
    byte[] CalcularHash(Cpf cpf);
}
