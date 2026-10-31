using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Campanhas.Infrastructure.Persistencia;

/// <summary>
/// Fábrica usada só pelas ferramentas do EF (<c>dotnet ef</c>) para criar migrations sem subir a API.
/// A conexão é fictícia e sem senha: nenhuma ferramenta de design conecta ao banco.
/// </summary>
public sealed class CampanhasDbContextFactory : IDesignTimeDbContextFactory<CampanhasDbContext>
{
    /// <inheritdoc/>
    public CampanhasDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<CampanhasDbContext>();
        CampanhasDbContext.Configurar(opcoes, "Host=localhost;Database=campanhas_db");
        return new CampanhasDbContext(opcoes.Options);
    }
}
