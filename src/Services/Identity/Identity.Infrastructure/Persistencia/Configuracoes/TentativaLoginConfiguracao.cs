using Identity.Domain.Autenticacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeia <see cref="TentativaLogin"/> para a tabela <c>tentativas_login</c>.
/// </summary>
public sealed class TentativaLoginConfiguracao : IEntityTypeConfiguration<TentativaLogin>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TentativaLogin> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tentativas_login");

        builder.HasKey(t => t.EmailHash);
        builder.Property(t => t.EmailHash).HasColumnName("email_hash").ValueGeneratedNever();
        builder.Property(t => t.Falhas).HasColumnName("falhas").IsRequired();
        builder.Property(t => t.JanelaInicio).HasColumnName("janela_inicio").IsRequired();
        builder.Property(t => t.BloqueadoAte).HasColumnName("bloqueado_ate");
    }
}
