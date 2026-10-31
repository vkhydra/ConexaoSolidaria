using Campanhas.Infrastructure.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeia <see cref="ChaveIdempotencia"/> para a tabela <c>chaves_idempotencia</c>, com chave primária composta por chave e doador.
/// </summary>
public sealed class ChaveIdempotenciaConfiguracao : IEntityTypeConfiguration<ChaveIdempotencia>
{
    /// <summary>
    /// Tamanho máximo da chave enviada pelo cliente.
    /// </summary>
    public const int ChaveTamanhoMaximo = 100;

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChaveIdempotencia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("chaves_idempotencia");

        builder.HasKey(c => new { c.Chave, c.DoadorId });
        builder.Property(c => c.Chave).HasMaxLength(ChaveTamanhoMaximo);
        builder.Property(c => c.HashCorpo).HasColumnType("bytea").IsRequired();
        builder.Property(c => c.CriadaEm).HasConversion(v => v.ToUniversalTime(), v => v);

        builder.HasIndex(c => c.CriadaEm);
    }
}
