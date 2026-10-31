using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeia o agregado <see cref="Campanha"/> para a tabela <c>campanhas</c>, com <c>xmin</c> como token de concorrência.
/// </summary>
public sealed class CampanhaConfiguracao : IEntityTypeConfiguration<Campanha>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Campanha> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("campanhas", tabela =>
        {
            tabela.HasCheckConstraint("ck_campanhas_periodo", "data_fim >= data_inicio");
            tabela.HasCheckConstraint("ck_campanhas_meta", "meta_financeira > 0");
            tabela.HasCheckConstraint("ck_campanhas_total", "total_arrecadado >= 0");
            tabela.HasCheckConstraint("ck_campanhas_status", "status IN ('Ativa', 'Concluida', 'Cancelada')");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion(id => id.Valor, valor => new CampanhaId(valor)).ValueGeneratedNever();

        builder.Property(c => c.Titulo).HasMaxLength(Campanha.TituloTamanhoMaximo).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(Campanha.DescricaoTamanhoMaximo).IsRequired();

        builder.ComplexProperty(c => c.Periodo, periodo =>
        {
            periodo.Property(p => p.DataInicio).HasColumnName("data_inicio").HasColumnType("date");
            periodo.Property(p => p.DataFim).HasColumnName("data_fim").HasColumnType("date");
        });

        builder.Property(c => c.MetaFinanceira)
            .HasColumnName("meta_financeira")
            .HasColumnType("numeric(18,2)")
            .HasConversion(d => d.Valor, v => Dinheiro.Criar(v).Valor);
        builder.Property(c => c.TotalArrecadado)
            .HasColumnName("total_arrecadado")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(Dinheiro.Zero)
            .HasConversion(d => d.Valor, v => Dinheiro.Criar(v).Valor);

        builder.Property(c => c.Status).HasMaxLength(20).HasConversion<string>().IsRequired();

        builder.Property(c => c.CriadoEm).HasConversion(v => v.ToUniversalTime(), v => v);
        builder.Property(c => c.AtualizadoEm).HasConversion(v => v.ToUniversalTime(), v => v);

        builder.Property(c => c.Versao).IsRowVersion();

        // O índice ix_campanhas_status_data_fim (status, data_fim) é criado por SQL na migration: o EF não indexa propriedades de tipos complexos.

        builder.Ignore(c => c.Eventos);
    }
}
