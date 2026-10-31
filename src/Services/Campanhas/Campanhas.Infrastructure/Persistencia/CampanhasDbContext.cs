using Campanhas.Domain.Campanhas;
using Campanhas.Infrastructure.Persistencia.Entidades;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Persistencia;

/// <summary>
/// Contexto do EF Core do banco <c>campanhas_db</c> (PostgreSQL), com as tabelas de campanhas e as do Outbox/Inbox do MassTransit.
/// </summary>
/// <param name="opcoes">Opções do contexto, com o provedor e a conexão.</param>
public sealed class CampanhasDbContext(DbContextOptions<CampanhasDbContext> opcoes) : DbContext(opcoes)
{
    /// <summary>
    /// Obtém as campanhas (tabela <c>campanhas</c>).
    /// </summary>
    public DbSet<Campanha> Campanhas => Set<Campanha>();

    /// <summary>
    /// Obtém as doações já somadas ao total (tabela <c>doacoes_processadas</c>).
    /// </summary>
    public DbSet<DoacaoProcessada> DoacoesProcessadas => Set<DoacaoProcessada>();

    /// <summary>
    /// Obtém as chaves de idempotência das doações (tabela <c>chaves_idempotencia</c>).
    /// </summary>
    public DbSet<ChaveIdempotencia> ChavesIdempotencia => Set<ChaveIdempotencia>();

    /// <summary>
    /// Aplica as opções comuns do contexto: PostgreSQL e nomes em <c>snake_case</c>.
    /// </summary>
    /// <param name="opcoes">Construtor de opções a configurar.</param>
    /// <param name="conexao">String de conexão do PostgreSQL.</param>
    /// <returns>O próprio construtor de opções, para encadeamento.</returns>
    public static DbContextOptionsBuilder Configurar(DbContextOptionsBuilder opcoes, string conexao)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        return opcoes.UseNpgsql(conexao).UseSnakeCaseNamingConvention();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampanhasDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
