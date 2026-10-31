using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistencia;

/// <summary>
/// Fábrica usada só pelas ferramentas do EF (<c>dotnet ef</c>) para criar migrations sem subir a API.
/// A conexão é fictícia e sem senha: nenhuma ferramenta de design conecta ao banco.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc/>
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=identity_db")
            .Options;

        return new IdentityDbContext(opcoes);
    }
}
