using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia.Configuracoes;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Repositório de usuários sobre o <see cref="IdentityDbContext"/>.
/// </summary>
/// <param name="contexto">Contexto do banco de identidade.</param>
/// <param name="cifrador">Cifrador e hash do CPF, usados ao gravar e ao conferir unicidade.</param>
public sealed class UsuarioRepositorio(IdentityDbContext contexto, ICifradorCpf cifrador) : IUsuarioRepositorio
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
    public Task<bool> ExisteEmailAsync(Email email, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(email);

        return contexto.Usuarios.AnyAsync(u => u.Email == email, cancelamento);
    }

    /// <inheritdoc/>
    public Task<bool> ExisteCpfAsync(byte[] cpfHash, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(cpfHash);

        return contexto.Usuarios.AnyAsync(u => EF.Property<byte[]?>(u, UsuarioConfiguracao.NomeCpfHash) == cpfHash, cancelamento);
    }

    /// <inheritdoc/>
    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var entrada = contexto.Usuarios.Add(usuario);
        if (usuario.Cpf is not null)
        {
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfCifrado).CurrentValue = cifrador.Cifrar(usuario.Cpf);
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfHash).CurrentValue = cifrador.CalcularHash(usuario.Cpf);
        }

        await contexto.SaveChangesAsync(cancelamento);
    }
}
