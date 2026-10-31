using Identity.Application.Abstracoes;
using Identity.Domain.Autenticacao;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    /// <remarks>
    /// Se duas falhas do mesmo e-mail novo chegam juntas, a segunda encontra a chave já gravada pela primeira
    /// (violação de unicidade). Nesse caso a falha concorrente é descartada: a contagem já foi iniciada e o login segue com 401.
    /// </remarks>
    public async Task SalvarAsync(TentativaLogin tentativa, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tentativa);

        var nova = contexto.Entry(tentativa).State == EntityState.Detached;
        if (nova)
        {
            contexto.TentativasLogin.Add(tentativa);
        }

        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException excecao) when (nova && excecao.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.Entry(tentativa).State = EntityState.Detached;
        }
    }
}
