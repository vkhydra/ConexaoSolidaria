using Campanhas.Application.Doacoes;
using Campanhas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Idempotencia;

/// <summary>
/// Consulta e limpeza das chaves de idempotência das doações, sobre o <see cref="CampanhasDbContext"/>.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas.</param>
public sealed class ChavesIdempotenciaRepositorio(CampanhasDbContext contexto) : IChavesIdempotencia
{
    /// <inheritdoc/>
    public async Task<ChaveRegistrada?> ObterAsync(string chave, Guid doadorId, CancellationToken cancelamento)
    {
        return await contexto.ChavesIdempotencia
            .AsNoTracking()
            .Where(c => c.Chave == chave && c.DoadorId == doadorId)
            .Select(c => new ChaveRegistrada(c.DoacaoId, c.HashCorpo))
            .FirstOrDefaultAsync(cancelamento);
    }

    /// <inheritdoc/>
    public Task<int> RemoverExpiradasAsync(DateTimeOffset limite, CancellationToken cancelamento)
    {
        var limiteUtc = limite.ToUniversalTime();
        return contexto.ChavesIdempotencia.Where(c => c.CriadaEm < limiteUtc).ExecuteDeleteAsync(cancelamento);
    }
}
