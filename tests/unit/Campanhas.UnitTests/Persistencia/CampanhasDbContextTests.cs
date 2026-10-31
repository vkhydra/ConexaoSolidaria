using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes do mapeamento do <see cref="CampanhasDbContext"/>: tabelas, colunas, restrições e tabelas do Outbox. O modelo é
/// montado sem conectar ao banco; a gravação real é coberta pelos testes de integração.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CampanhasDbContextTests
{
    /// <summary>
    /// Contexto criado pela fábrica de design-time, sem abrir conexão.
    /// </summary>
    private readonly CampanhasDbContext _contexto = new CampanhasDbContextFactory().CreateDbContext([]);

    /// <summary>
    /// Modelo completo (de design), que guarda restrições e conversores.
    /// </summary>
    private readonly IModel _modelo;

    /// <summary>
    /// Obtém o modelo de design do contexto.
    /// </summary>
    public CampanhasDbContextTests()
    {
        _modelo = _contexto.GetService<IDesignTimeModel>().Model;
    }

    /// <summary>
    /// Dado o modelo do contexto, quando consultado, então <c>campanhas</c> tem as colunas, as restrições e o token xmin.
    /// </summary>
    [Fact]
    public void Modelo_Campanha_MapeiaTabelaColunasERestricoes()
    {
        var tipo = _modelo.FindEntityType(typeof(Campanha))!;

        tipo.GetTableName().ShouldBe("campanhas");
        tipo.FindPrimaryKey()!.Properties.Single().GetColumnName().ShouldBe("id");
        tipo.GetProperties().Select(p => p.GetColumnName()).ShouldBe(
            ["id", "titulo", "descricao", "meta_financeira", "total_arrecadado", "status", "criado_em", "atualizado_em", "xmin"],
            ignoreOrder: true);
        tipo.FindComplexProperty(nameof(Campanha.Periodo))!.ComplexType.GetProperties().Select(p => p.GetColumnName())
            .ShouldBe(["data_inicio", "data_fim"], ignoreOrder: true);
        tipo.GetCheckConstraints().Select(c => c.Name).ShouldBe(
            ["ck_campanhas_periodo", "ck_campanhas_meta", "ck_campanhas_total", "ck_campanhas_status"],
            ignoreOrder: true);
        tipo.FindProperty(nameof(Campanha.Versao))!.IsConcurrencyToken.ShouldBeTrue();
        tipo.FindProperty(nameof(Campanha.Titulo))!.GetMaxLength().ShouldBe(120);
        tipo.FindProperty(nameof(Campanha.Descricao))!.GetMaxLength().ShouldBe(2000);
    }

    /// <summary>
    /// Dado o modelo do contexto, quando consultado, então as tabelas do Outbox e do Inbox do MassTransit estão em snake_case.
    /// </summary>
    [Fact]
    public void Modelo_Outbox_TemTabelasDoMassTransit()
    {
        var tabelas = _modelo.GetEntityTypes().Select(t => t.GetTableName()).ToList();

        tabelas.ShouldContain("outbox_message");
        tabelas.ShouldContain("outbox_state");
        tabelas.ShouldContain("inbox_state");
    }

    /// <summary>
    /// Dados valores do domínio, quando convertidos para o banco e de volta, então identificador, dinheiro e instantes não mudam.
    /// </summary>
    [Fact]
    public void Conversores_IdDinheiroEInstantes_FazemIdaEVolta()
    {
        var tipo = _modelo.FindEntityType(typeof(Campanha))!;
        var id = CampanhaId.Novo();

        var conversorId = tipo.FindProperty(nameof(Campanha.Id))!.GetValueConverter()!;
        conversorId.ConvertToProvider(id).ShouldBe(id.Valor);
        conversorId.ConvertFromProvider(id.Valor).ShouldBe(id);

        var conversorMeta = tipo.FindProperty(nameof(Campanha.MetaFinanceira))!.GetValueConverter()!;
        var dinheiro = Dinheiro.Criar(1234.56m).Valor;
        conversorMeta.ConvertToProvider(dinheiro).ShouldBe(1234.56m);
        conversorMeta.ConvertFromProvider(1234.56m).ShouldBe(dinheiro);

        var conversorData = tipo.FindProperty(nameof(Campanha.CriadoEm))!.GetValueConverter()!;
        var local = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));
        ((DateTimeOffset)conversorData.ConvertToProvider(local)!).Offset.ShouldBe(TimeSpan.Zero);
        tipo.FindProperty(nameof(Campanha.Status))!.GetProviderClrType().ShouldBe(typeof(string));
    }

    /// <summary>
    /// Dado um construtor de opções nulo ou um de modelo nulo, quando usados, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Configurar_ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => CampanhasDbContext.Configurar(null!, "Host=localhost"));
        Should.Throw<ArgumentNullException>(() => new Infrastructure.Persistencia.Configuracoes.CampanhaConfiguracao().Configure(null!));
    }
}
