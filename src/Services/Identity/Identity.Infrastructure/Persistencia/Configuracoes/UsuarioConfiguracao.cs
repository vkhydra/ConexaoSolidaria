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

    /// <summary>
    /// Nome da propriedade de sombra com o CPF cifrado (coluna <c>cpf_cifrado</c>).
    /// </summary>
    public const string NomeCpfCifrado = "CpfCifrado";

    /// <summary>
    /// Nome da propriedade de sombra com o hash do CPF (coluna <c>cpf_hash</c>).
    /// </summary>
    public const string NomeCpfHash = "CpfHash";

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
            .HasConversion(email => email.Valor, texto => Email.Restaurar(texto))
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_usuarios_email");

        builder.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(100);
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(u => u.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(u => u.AnonimizadoEm).HasColumnName("anonimizado_em");

        builder.Ignore(u => u.EstaAnonimizado);

        // O CPF não é uma coluna do agregado: o repositório grava o cifrado e o hash nestas colunas de sombra.
        builder.Ignore(u => u.Cpf);
        builder.Property<byte[]?>(NomeCpfCifrado).HasColumnName("cpf_cifrado");
        builder.Property<byte[]?>(NomeCpfHash).HasColumnName("cpf_hash");
        builder.HasIndex(NomeCpfHash).IsUnique().HasDatabaseName("ux_usuarios_cpf_hash").HasFilter("cpf_hash IS NOT NULL");

        builder.ComplexProperty(u => u.Consentimento, consentimento =>
        {
            consentimento.Property(c => c.Versao).HasColumnName("consentimento_versao").HasMaxLength(Consentimento.TamanhoMaximoVersao);
            consentimento.Property(c => c.Em).HasColumnName("consentimento_em");
        });
    }
}
