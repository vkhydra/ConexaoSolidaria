using Identity.Domain.Autenticacao;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia;
using Identity.Infrastructure.Persistencia.Configuracoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Identity.UnitTests.Persistencia;

/// <summary>
/// Testes do mapeamento do <see cref="IdentityDbContext"/>: tabelas, colunas, chaves e restrições. O modelo é
/// montado sem conectar ao banco; a gravação real é coberta pelos testes de integração.
/// </summary>
[Trait("Category", "Unit")]
public sealed class IdentityDbContextTests
{
    /// <summary>
    /// Contexto criado pela fábrica de design-time, sem abrir conexão.
    /// </summary>
    private readonly IdentityDbContext _contexto = new IdentityDbContextFactory().CreateDbContext([]);

    /// <summary>
    /// Modelo completo (de design), que guarda restrições e conversores.
    /// </summary>
    private readonly IModel _modelo;

    /// <summary>
    /// Obtém o modelo de design do contexto.
    /// </summary>
    public IdentityDbContextTests()
    {
        _modelo = _contexto.GetService<IDesignTimeModel>().Model;
    }

    /// <summary>
    /// Dado o modelo do contexto, quando consultado, então <c>usuarios</c> tem as colunas, o índice único de e-mail e a restrição de papel.
    /// </summary>
    [Fact]
    public void Modelo_Usuario_MapeiaTabelaColunasEIndices()
    {
        var tipo = _modelo.FindEntityType(typeof(Usuario))!;

        tipo.GetTableName().ShouldBe("usuarios");
        tipo.FindPrimaryKey()!.Properties.Single().Name.ShouldBe(nameof(Usuario.Id));
        tipo.GetProperties().Select(p => p.GetColumnName()).ShouldBe(
            ["id", "nome_completo", "email", "senha_hash", "role", "criado_em", "anonimizado_em", "cpf_cifrado", "cpf_hash"],
            ignoreOrder: true);
        tipo.FindComplexProperty(nameof(Usuario.Consentimento))!.ComplexType.GetProperties().Select(p => p.GetColumnName())
            .ShouldBe(["consentimento_versao", "consentimento_em"], ignoreOrder: true);
        tipo.FindProperty(nameof(Usuario.SenhaHash))!.IsNullable.ShouldBeTrue();
        tipo.FindProperty(nameof(Usuario.Email))!.GetMaxLength().ShouldBe(Email.TamanhoMaximo);

        var indices = tipo.GetIndexes().ToDictionary(i => i.GetDatabaseName()!);
        indices.Keys.ShouldBe(["ux_usuarios_email", "ux_usuarios_cpf_hash"], ignoreOrder: true);
        indices.Values.ShouldAllBe(i => i.IsUnique);
        indices["ux_usuarios_cpf_hash"].GetFilter().ShouldBe("cpf_hash IS NOT NULL");
        tipo.GetCheckConstraints().Single().Name.ShouldBe(UsuarioConfiguracao.RestricaoRole);
    }

    /// <summary>
    /// Dado o modelo do contexto, quando consultado, então <c>tentativas_login</c> usa o hash do e-mail como chave.
    /// </summary>
    [Fact]
    public void Modelo_TentativaLogin_MapeiaTabelaEChave()
    {
        var tipo = _modelo.FindEntityType(typeof(TentativaLogin))!;

        tipo.GetTableName().ShouldBe("tentativas_login");
        tipo.FindPrimaryKey()!.Properties.Single().GetColumnName().ShouldBe("email_hash");
        tipo.GetProperties().Select(p => p.GetColumnName()).ShouldBe(
            ["email_hash", "falhas", "janela_inicio", "bloqueado_ate"],
            ignoreOrder: true);
    }

    /// <summary>
    /// Dados valores do domínio, quando convertidos para o banco e de volta, então e-mail, identificador e papel não mudam.
    /// </summary>
    [Fact]
    public void Conversores_IdEmailERole_FazemIdaEVolta()
    {
        var tipo = _modelo.FindEntityType(typeof(Usuario))!;
        var id = UsuarioId.Novo();
        var email = Email.Criar("gestor@exemplo.com").Valor;

        var conversorId = tipo.FindProperty(nameof(Usuario.Id))!.GetValueConverter()!;
        conversorId.ConvertToProvider(id).ShouldBe(id.Valor);
        conversorId.ConvertFromProvider(id.Valor).ShouldBe(id);

        var conversorEmail = tipo.FindProperty(nameof(Usuario.Email))!.GetValueConverter()!;
        conversorEmail.ConvertToProvider(email).ShouldBe("gestor@exemplo.com");
        conversorEmail.ConvertFromProvider("gestor@exemplo.com").ShouldBe(email);

        tipo.FindProperty(nameof(Usuario.Role))!.GetProviderClrType().ShouldBe(typeof(string));
    }

    /// <summary>
    /// Dado um construtor de modelo nulo, quando o contexto ou uma configuração o recebe, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Configuracoes_ConstrutorNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new UsuarioConfiguracao().Configure(null!));
        Should.Throw<ArgumentNullException>(() => new TentativaLoginConfiguracao().Configure(null!));
    }
}
