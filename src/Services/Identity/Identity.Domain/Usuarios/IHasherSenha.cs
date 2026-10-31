namespace Identity.Domain.Usuarios;

/// <summary>
/// Porta do domínio para o hash de senhas. A implementação (BCrypt) fica na infraestrutura.
/// </summary>
public interface IHasherSenha
{
    /// <summary>
    /// Gera o hash de uma senha.
    /// </summary>
    /// <param name="senha">Senha em texto, que nunca é guardada.</param>
    /// <returns>Hash com o sal embutido.</returns>
    string Hash(string senha);

    /// <summary>
    /// Confere uma senha contra um hash guardado.
    /// </summary>
    /// <param name="senha">Senha em texto.</param>
    /// <param name="hash">Hash guardado do usuário.</param>
    /// <returns><c>true</c> se a senha corresponde ao hash.</returns>
    bool Verificar(string senha, string hash);

    /// <summary>
    /// Faz o mesmo trabalho de uma verificação contra um hash fictício e sempre devolve falso. Serve para
    /// que o login de um e-mail inexistente demore o mesmo tempo que o de um e-mail existente.
    /// </summary>
    /// <param name="senha">Senha em texto.</param>
    /// <returns>Sempre <c>false</c>.</returns>
    bool VerificarSemUsuario(string senha);
}
