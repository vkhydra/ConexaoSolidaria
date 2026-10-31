using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Repositório de usuários sobre o <see cref="IdentityDbContext"/>.
/// </summary>
/// <param name="contexto">Contexto do banco de identidade.</param>
public sealed class UsuarioRepositorio(IdentityDbContext contexto) : IUsuarioRepositorio
{
    /// <inheritdoc/>
    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(email);

        return contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancelamento);
    }

    /// <inheritdoc/>
    public Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancelamento)
    {
        return contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public Task<bool> ExisteGestorAsync(CancellationToken cancelamento)
    {
        return contexto.Usuarios.AnyAsync(u => u.Role == Role.GestorONG, cancelamento);
    }

    /// <inheritdoc/>
    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync(cancelamento);
    }
}
