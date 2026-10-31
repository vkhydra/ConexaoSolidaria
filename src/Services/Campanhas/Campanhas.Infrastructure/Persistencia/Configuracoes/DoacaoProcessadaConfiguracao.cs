using Campanhas.Domain.Campanhas;
using Campanhas.Infrastructure.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeia <see cref="DoacaoProcessada"/> para a tabela <c>doacoes_processadas</c>, ligada à campanha por chave estrangeira.
/// </summary>
public sealed class DoacaoProcessadaConfiguracao : IEntityTypeConfiguration<DoacaoProcessada>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DoacaoProcessada> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("doacoes_processadas", tabela => tabela.HasCheckConstraint("ck_doacoes_processadas_valor", "valor > 0"));

        builder.HasKey(d => d.DoacaoId);
        builder.Property(d => d.DoacaoId).ValueGeneratedNever();
        builder.Property(d => d.CampanhaId).HasConversion(id => id.Valor, valor => new CampanhaId(valor)).IsRequired();
        builder.Property(d => d.Valor).HasColumnType("numeric(18,2)");
        builder.Property(d => d.ProcessadaEm).HasConversion(v => v.ToUniversalTime(), v => v);

        builder.HasOne<Campanha>().WithMany().HasForeignKey(d => d.CampanhaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.CampanhaId);
    }
}
