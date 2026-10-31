using Identity.Domain.Autenticacao;
using Identity.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistencia;

/// <summary>
/// Contexto do EF Core do banco <c>identity_db</c> (PostgreSQL).
/// </summary>
/// <param name="opcoes">Opções do contexto, com o provedor e a conexão.</param>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> opcoes) : DbContext(opcoes)
{
    /// <summary>
    /// Obtém os usuários (tabela <c>usuarios</c>).
    /// </summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>
    /// Obtém o controle de tentativas de login (tabela <c>tentativas_login</c>).
    /// </summary>
    public DbSet<TentativaLogin> TentativasLogin => Set<TentativaLogin>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
