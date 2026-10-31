using Identity.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeia o agregado <see cref="Usuario"/> para a tabela <c>usuarios</c>.
/// </summary>
public sealed class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    /// <summary>
    /// Nome da restrição que limita os papéis aceitos.
    /// </summary>
    public const string RestricaoRole = "ck_usuarios_role";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuarios", tabela => tabela.HasCheckConstraint(RestricaoRole, "role IN ('GestorONG', 'Doador')"));

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new UsuarioId(valor))
            .ValueGeneratedNever();

        builder.Property(u => u.NomeCompleto).HasColumnName("nome_completo").HasMaxLength(150).IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.TamanhoMaximo)
            .HasConversion(email => email.Valor, texto => Email.Criar(texto).Valor)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_usuarios_email");

        builder.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(100);
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(u => u.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(u => u.AnonimizadoEm).HasColumnName("anonimizado_em");

        builder.Ignore(u => u.EstaAnonimizado);

        // Mapeados na migration de cadastro de doador (CPF cifrado e consentimento); até lá ficam fora do modelo.
        builder.Ignore(u => u.Cpf);
        builder.Ignore(u => u.Consentimento);
    }
}
