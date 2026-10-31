using Identity.Domain.Usuarios;

namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta de persistência dos usuários. A implementação (EF Core e PostgreSQL) fica na infraestrutura.
/// </summary>
public interface IUsuarioRepositorio
{
    /// <summary>
    /// Busca um usuário pelo e-mail normalizado.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O usuário ou nulo se não existir.</returns>
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancelamento);

    /// <summary>
    /// Busca um usuário pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O usuário ou nulo se não existir.</returns>
    Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancelamento);

    /// <summary>
    /// Lê e decifra o CPF de um usuário.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O CPF, ou nulo se o usuário não tem CPF (gestor ou anonimizado).</returns>
    Task<Cpf?> ObterCpfAsync(UsuarioId id, CancellationToken cancelamento);

    /// <summary>
    /// Grava as alterações de um usuário já carregado. Se o usuário foi anonimizado, o CPF cifrado e o hash são apagados
    /// na mesma gravação.
    /// </summary>
    /// <param name="usuario">Usuário carregado por este repositório e alterado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando as alterações foram gravadas.</returns>
    Task SalvarAsync(Usuario usuario, CancellationToken cancelamento);

    /// <summary>
    /// Informa se já existe algum usuário com o papel de gestor.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><c>true</c> se há pelo menos um gestor.</returns>
    Task<bool> ExisteGestorAsync(CancellationToken cancelamento);

    /// <summary>
    /// Informa se o e-mail já está em uso.
    /// </summary>
    /// <param name="email">E-mail normalizado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><c>true</c> se existe um usuário com esse e-mail.</returns>
    Task<bool> ExisteEmailAsync(Email email, CancellationToken cancelamento);

    /// <summary>
    /// Informa se o CPF já está em uso, comparando o hash.
    /// </summary>
    /// <param name="cpfHash">HMAC-SHA256 do CPF.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><c>true</c> se existe um usuário com esse CPF.</returns>
    Task<bool> ExisteCpfAsync(byte[] cpfHash, CancellationToken cancelamento);

    /// <summary>
    /// Adiciona um usuário novo e salva.
    /// </summary>
    /// <param name="usuario">Usuário a gravar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o usuário foi gravado.</returns>
    /// <exception cref="UnicidadeVioladaException">Outro usuário gravou o mesmo e-mail ou CPF no mesmo instante.</exception>
    Task AdicionarAsync(Usuario usuario, CancellationToken cancelamento);
}
