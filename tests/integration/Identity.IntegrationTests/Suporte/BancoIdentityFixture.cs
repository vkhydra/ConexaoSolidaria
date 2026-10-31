using Identity.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Identity.IntegrationTests.Suporte;

/// <summary>
/// Sobe um PostgreSQL real em container, aplica as migrations do Identity e entrega contextos novos aos testes.
/// </summary>
public sealed class BancoIdentityFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do PostgreSQL.
    /// </summary>
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Sobe o container e aplica as migrations.
    /// </summary>
    /// <returns>Tarefa que termina com o banco pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    /// <summary>
    /// Cria um contexto novo, com o próprio rastreamento de entidades, ligado ao banco do container.
    /// </summary>
    /// <returns>Contexto que o chamador deve descartar.</returns>
    public IdentityDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new IdentityDbContext(opcoes);
    }

    /// <summary>
    /// Remove o container.
    /// </summary>
    /// <returns>Tarefa que termina com o container removido.</returns>
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
