using Identity.Application.Abstracoes;
using Identity.Domain.Autenticacao;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Repositório do controle de tentativas de login sobre o <see cref="IdentityDbContext"/>.
/// </summary>
/// <param name="contexto">Contexto do banco de identidade.</param>
public sealed class TentativaLoginRepositorio(IdentityDbContext contexto) : ITentativaLoginRepositorio
{
    /// <inheritdoc/>
    public Task<TentativaLogin?> ObterAsync(byte[] emailHash, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(emailHash);

        return contexto.TentativasLogin.FirstOrDefaultAsync(t => t.EmailHash == emailHash, cancelamento);
    }

    /// <inheritdoc/>
    public async Task SalvarAsync(TentativaLogin tentativa, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tentativa);

        if (contexto.Entry(tentativa).State == EntityState.Detached)
        {
            contexto.TentativasLogin.Add(tentativa);
        }

        await contexto.SaveChangesAsync(cancelamento);
    }
}
