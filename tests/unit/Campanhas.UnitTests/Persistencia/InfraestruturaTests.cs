using Campanhas.Application.Arrecadacao;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Mensageria;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes dos componentes de infraestrutura sem banco: relógio de São Paulo, mapeamento do evento e registro do MassTransit.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InfraestruturaTests
{
    /// <summary>
    /// Dado um instante às 02:00 UTC, quando se pergunta o dia, então em São Paulo ainda é o dia anterior.
    /// </summary>
    [Fact]
    public void RelogioSaoPaulo_Hoje_UsaOFusoDeSaoPaulo()
    {
        var relogio = new RelogioSaoPaulo(new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 2, 0, 0, TimeSpan.Zero)));

        relogio.Hoje().ShouldBe(new DateOnly(2026, 11, 10));
        relogio.Agora().Offset.ShouldBe(TimeSpan.FromHours(-3));
        relogio.Agora().UtcDateTime.ShouldBe(new DateTime(2026, 11, 11, 2, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Dado um instante às 12:00 UTC, quando se pergunta o dia, então a data é a mesma em UTC e em São Paulo.
    /// </summary>
    [Fact]
    public void RelogioSaoPaulo_Hoje_MeioDoDiaMantemAData()
    {
        var relogio = new RelogioSaoPaulo(new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero)));

        relogio.Hoje().ShouldBe(new DateOnly(2026, 11, 11));
    }

    /// <summary>
    /// Dada uma campanha, quando o evento de integração é montado, então leva o estado atual e a versão em ticks UTC.
    /// </summary>
    [Fact]
    public void EventosDeIntegracao_ParaAlterada_LevaOEstadoAtual()
    {
        var agora = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));
        var campanha = Campanha.Criar(
            "Natal",
            "Cestas",
            Periodo.Criar(new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 24)).Valor,
            Dinheiro.Criar(500m).Valor,
            new DateOnly(2026, 11, 10),
            agora).Valor;

        var evento = EventosDeIntegracao.ParaAlterada(campanha);

        evento.CampanhaId.ShouldBe(campanha.Id.Valor);
        evento.Titulo.ShouldBe("Natal");
        evento.Descricao.ShouldBe("Cestas");
        evento.Status.ShouldBe("Ativa");
        evento.MetaFinanceira.ShouldBe(500m);
        evento.DataFim.ShouldBe(new DateOnly(2026, 12, 24));
        evento.Versao.ShouldBe(agora.UtcTicks);
        Should.Throw<ArgumentNullException>(() => EventosDeIntegracao.ParaAlterada(null!));
    }

    /// <summary>
    /// Dada a conexão do MongoDB ausente ou argumentos nulos, quando o registro da arrecadação é feito, então a aplicação não sobe.
    /// </summary>
    [Fact]
    public void AddCampanhasArrecadacao_ConfiguracaoInvalida_LancaExcecao()
    {
        var semMongo = new ConfigurationBuilder().Build();

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddCampanhasArrecadacao(semMongo));
        Should.Throw<ArgumentNullException>(() => ExtensoesCampanhasInfraestrutura.AddCampanhasArrecadacao(null!, semMongo));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddCampanhasArrecadacao(null!));
    }

    /// <summary>
    /// Dada a conexão do MongoDB, quando o registro da arrecadação é feito, então o livro e o registro entram no contêiner.
    /// </summary>
    [Fact]
    public void AddCampanhasArrecadacao_ComMongo_RegistraLivroERegistro()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Mongo"] = "mongodb://mongo.invalido.local:27017" }).Build();
        var servicos = new ServiceCollection();

        var retorno = servicos.AddCampanhasArrecadacao(configuracao);

        retorno.ShouldBeSameAs(servicos);
        servicos.ShouldContain(d => d.ServiceType == typeof(ILivroDoacoes));
        servicos.ShouldContain(d => d.ServiceType == typeof(IRegistroArrecadacao));
        servicos.ShouldContain(d => d.ServiceType == typeof(IMetricasArrecadacao));
    }

    /// <summary>
    /// Dado o host do RabbitMQ ausente ou argumentos nulos, quando a mensageria é registrada, então a aplicação não sobe.
    /// </summary>
    [Fact]
    public void AddCampanhasMensageria_ConfiguracaoInvalida_LancaExcecao()
    {
        var semHost = new ConfigurationBuilder().Build();

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddCampanhasMensageria(semHost));
        Should.Throw<ArgumentNullException>(() => ConfiguracaoMassTransit.AddCampanhasMensageria(null!, semHost));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddCampanhasMensageria(null!));
        Should.Throw<ArgumentNullException>(() => ConfiguracaoMassTransit.ConfigurarOutbox(null!));
    }

    /// <summary>
    /// Dada a configuração do RabbitMQ, com ou sem credenciais, quando a mensageria é registrada, então os serviços entram no contêiner.
    /// </summary>
    /// <param name="comCredenciais">Se usuário e senha vêm da configuração ou usam o padrão.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddCampanhasMensageria_ComHost_RegistraServicos(bool comCredenciais)
    {
        var valores = new Dictionary<string, string?> { ["RabbitMq:Host"] = "rabbit.local" };
        if (comCredenciais)
        {
            valores["RabbitMq:Usuario"] = "app";
            valores["RabbitMq:Senha"] = "segredo-de-teste";
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicos = new ServiceCollection();

        var retorno = servicos.AddCampanhasMensageria(configuracao);

        retorno.ShouldBeSameAs(servicos);
        servicos.Count.ShouldBeGreaterThan(0);
    }
}
