using Arrecadacao.Worker.Consumidores;
using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Transparencia;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Arrecadacao.Worker.UnitTests;

/// <summary>
/// Testes do <see cref="DoacaoRecebidaConsumer"/> com o Test Harness do MassTransit (transporte em memória): sucesso e falha
/// permanente. A política de retentativa e o Outbox do EF Core são testados nos testes de integração, com banco real.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DoacaoRecebidaConsumerTests
{
    /// <summary>
    /// Instante do processamento nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 10, 6, TimeSpan.FromHours(-3));

    /// <summary>
    /// Porta de registro falsa.
    /// </summary>
    private readonly IRegistroArrecadacao _registro = Substitute.For<IRegistroArrecadacao>();

    /// <summary>
    /// Livro de doações falso.
    /// </summary>
    private readonly ILivroDoacoes _livro = Substitute.For<ILivroDoacoes>();

    /// <summary>
    /// Dada uma doação válida, quando a mensagem é consumida, então o consumidor termina sem falha e o livro é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_DoacaoValida_ProcessaSemFalha()
    {
        var doacao = NovaDoacao();
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.Registrada, _agora, 150m));
        await using var provedor = CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);

        (await harness.Consumed.Any<DoacaoRecebidaEvent>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await harness.GetConsumerHarness<DoacaoRecebidaConsumer>().Consumed.Any<DoacaoRecebidaEvent>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await harness.Published.Any<Fault<DoacaoRecebidaEvent>>(TestContext.Current.CancellationToken)).ShouldBeFalse();
        await _livro.Received(1).RegistrarAsync(Arg.Is<LancamentoDoacao>(l => l.DoacaoId == doacao.DoacaoId), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma doação de campanha inexistente, quando a mensagem é consumida, então o consumidor falha com a exceção
    /// permanente (que a política de retentativa não repete) e o livro não é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_CampanhaInexistente_FalhaComExcecaoPermanente()
    {
        var doacao = NovaDoacao();
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.CampanhaInexistente, null, null));
        await using var provedor = CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);

        var falha = (await harness.Published.SelectAsync<Fault<DoacaoRecebidaEvent>>(TestContext.Current.CancellationToken).FirstOrDefault())!;
        falha.Context.Message.Exceptions.Single().ExceptionType.ShouldBe(typeof(DoacaoNaoProcessavelException).FullName);
        falha.Context.Message.Exceptions.Single().Message.ShouldContain("campanha.nao_encontrada");
        await _livro.DidNotReceive().RegistrarAsync(Arg.Any<LancamentoDoacao>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada a política padrão, quando consultada, então tem 3 retentativas imediatas e reentregas após 5 s, 30 s e 2 min.
    /// </summary>
    [Fact]
    public void PoliticaPadrao_ProdutoDefinido_TemOsIntervalosDoPlano()
    {
        PoliticaDeRetentativa.Padrao.Imediatas.ShouldBe(3);
        PoliticaDeRetentativa.Padrao.Intervalos.ShouldBe([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)]);
        DoacaoRecebidaConsumerDefinition.NomeDaFila.ShouldBe("doacao-recebida");
        DoacaoRecebidaConsumerDefinition.Prefetch.ShouldBe(16);
    }

    /// <summary>
    /// Monta o contêiner de serviços com o consumidor, o Test Harness e as dependências falsas.
    /// </summary>
    /// <returns>O provedor de serviços.</returns>
    private ServiceProvider CriarProvedor()
    {
        var relogio = Substitute.For<IRelogio>();
        relogio.Agora().Returns(_agora);
        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(_registro)
            .AddSingleton(_livro)
            .AddSingleton(Substitute.For<ICachePainel>())
            .AddSingleton(relogio)
            .AddSingleton<InvalidadorDoPainel>()
            .AddScoped<ProcessarDoacao>()
            .AddMassTransitTestHarness(registro => registro.AddConsumer<DoacaoRecebidaConsumer>())
            .BuildServiceProvider(true);
    }

    /// <summary>
    /// Cria uma doação recebida com identificadores novos.
    /// </summary>
    /// <returns>O evento de doação recebida.</returns>
    private static DoacaoRecebidaEvent NovaDoacao()
    {
        return new DoacaoRecebidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 150m, _agora.AddSeconds(-1));
    }
}
