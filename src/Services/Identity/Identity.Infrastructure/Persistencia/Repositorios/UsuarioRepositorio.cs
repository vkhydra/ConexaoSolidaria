using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia.Configuracoes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    public async Task<Cpf?> ObterCpfAsync(UsuarioId id, CancellationToken cancelamento)
    {
        var cifrado = await contexto.Usuarios
            .Where(u => u.Id == id)
            .Select(u => EF.Property<byte[]?>(u, UsuarioConfiguracao.NomeCpfCifrado))
            .SingleOrDefaultAsync(cancelamento);

        return cifrado is null ? null : cifrador.Decifrar(cifrado);
    }

    /// <inheritdoc/>
    public async Task SalvarAsync(Usuario usuario, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (usuario.EstaAnonimizado)
        {
            var entrada = contexto.Entry(usuario);
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfCifrado).CurrentValue = null;
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfHash).CurrentValue = null;
        }

        await contexto.SaveChangesAsync(cancelamento);
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
    /// <exception cref="UnicidadeVioladaException">Outro usuário gravou o mesmo e-mail ou CPF no mesmo instante.</exception>
    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var entrada = contexto.Usuarios.Add(usuario);
        if (usuario.Cpf is not null)
        {
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfCifrado).CurrentValue = cifrador.Cifrar(usuario.Cpf);
            entrada.Property<byte[]?>(UsuarioConfiguracao.NomeCpfHash).CurrentValue = cifrador.CalcularHash(usuario.Cpf);
        }

        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            entrada.State = EntityState.Detached;
            throw pg.ConstraintName switch
            {
                "ux_usuarios_email" => new UnicidadeVioladaException(CampoUnico.Email, excecao),
                "ux_usuarios_cpf_hash" => new UnicidadeVioladaException(CampoUnico.Cpf, excecao),
                _ => excecao,
            };
        }
    }
}
