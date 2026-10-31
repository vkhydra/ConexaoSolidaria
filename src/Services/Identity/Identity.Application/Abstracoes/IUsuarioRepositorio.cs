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
    /// Informa se já existe algum usuário com o papel de gestor.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><c>true</c> se há pelo menos um gestor.</returns>
    Task<bool> ExisteGestorAsync(CancellationToken cancelamento);

    /// <summary>
    /// Adiciona um usuário novo e salva.
    /// </summary>
    /// <param name="usuario">Usuário a gravar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o usuário foi gravado.</returns>
    Task AdicionarAsync(Usuario usuario, CancellationToken cancelamento);
}
