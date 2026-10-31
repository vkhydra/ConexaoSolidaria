using Arrecadacao.Worker.IntegrationTests.Suporte;
using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure.Arrecadacao;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Arrecadacao.Worker.IntegrationTests;

/// <summary>
/// Testes do Worker inteiro: consumidor com a definição de produção, Outbox do EF Core, PostgreSQL e MongoDB reais, sobre o
/// Test Harness do MassTransit. Cobrem soma, duplicata, livro na reentrega, falha transitória, falha permanente e a
/// conferência da soma do livro com o total.
/// </summary>
/// <param name="ambiente">Bancos compartilhados pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class ConsumidorTests(ConsumidorFixture ambiente) : IClassFixture<ConsumidorFixture>
{
    /// <summary>
    /// Instante em que a doação foi aceita, nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _recebida = new(2026, 11, 10, 14, 10, 5, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dada uma doação nova, quando a mensagem é consumida, então o total sobe, o livro recebe o lançamento e o
    /// DoacaoProcessadaEvent é publicado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_DoacaoNova_SomaGravaLivroEPublicaProcessada()
    {
        var campanhaId = await ambiente.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 150.00m);
        await using var provedor = ambiente.CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);

        (await harness.Consumed.Any<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacao.DoacaoId, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await ambiente.LerTotalAsync(campanhaId)).ShouldBe(150.00m);
        var lancamento = (await ambiente.LerLivroAsync(campanhaId)).ShouldHaveSingleItem();
        lancamento["_id"].AsString.ShouldBe(doacao.DoacaoId.ToString());
        lancamento["totalAposLancamento"].ToDecimal().ShouldBe(150.00m);
        var processada = (await harness.Consumed.SelectAsync<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacao.DoacaoId, TestContext.Current.CancellationToken).First()).Context.Message;
        processada.TotalArrecadado.ShouldBe(150.00m);
        processada.DoadorId.ShouldBe(doacao.DoadorId);
        processada.RecebidaEm.ShouldBe(doacao.OcorridoEm);
    }

    /// <summary>
    /// Dada a mesma doação entregue duas vezes (mensagens diferentes, mesmo identificador de doação), quando consumidas,
    /// então a soma, o livro e o evento de processada acontecem uma única vez.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_Duplicata_SomaUmaUnicaVez()
    {
        var campanhaId = await ambiente.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 80.00m);
        await using var provedor = ambiente.CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);
        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 2);

        (await ambiente.LerTotalAsync(campanhaId)).ShouldBe(80.00m);
        (await ambiente.LerLivroAsync(campanhaId)).ShouldHaveSingleItem();
        harness.Consumed.Select<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacao.DoacaoId, TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    /// <summary>
    /// Dada uma doação já somada no PostgreSQL cujo lançamento nunca chegou ao livro, quando a mensagem é reentregue, então
    /// o total não muda mas o livro é completado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_ReentregaSemLancamentoNoLivro_CompletaOLivroSemSomarDeNovo()
    {
        var campanhaId = await ambiente.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 60.00m);
        await ambiente.RegistrarSemLivroAsync(doacao);
        (await ambiente.LerLivroAsync(campanhaId)).ShouldBeEmpty();
        await using var provedor = ambiente.CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);
        await EsperarConsumoAsync(harness, 1);

        (await ambiente.LerTotalAsync(campanhaId)).ShouldBe(60.00m);
        var lancamento = (await ambiente.LerLivroAsync(campanhaId)).ShouldHaveSingleItem();
        lancamento["_id"].AsString.ShouldBe(doacao.DoacaoId.ToString());
    }

    /// <summary>
    /// Dado um livro que falha nas duas primeiras gravações, quando a mensagem é consumida, então a retentativa a processa
    /// e o total reflete uma única soma (a transação da tentativa que falhou foi desfeita).
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_FalhaTransitoria_RetentaESomaUmaUnicaVez()
    {
        var campanhaId = await ambiente.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 150.00m);
        var livro = new LivroInstavel(2);
        await using var provedor = ambiente.CriarProvedor(servicos => servicos.AddSingleton<ILivroDoacoes>(sp =>
        {
            livro.Real = new LivroDoacoesMongo(sp.GetRequiredService<IMongoDatabase>());
            return livro;
        }));
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);

        (await harness.Consumed.Any<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacao.DoacaoId, TestContext.Current.CancellationToken)).ShouldBeTrue();
        livro.Tentativas.ShouldBeGreaterThanOrEqualTo(3);
        (await ambiente.LerTotalAsync(campanhaId)).ShouldBe(150.00m);
        (await ambiente.LerLivroAsync(campanhaId)).ShouldHaveSingleItem();
        harness.Consumed.Select<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacao.DoacaoId, TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    /// <summary>
    /// Dada uma doação de campanha inexistente, quando a mensagem é consumida, então falha uma única vez, sem retentativas,
    /// e vai para a fila de erro; nada é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_FalhaPermanente_VaiParaFilaDeErroSemRetentativas()
    {
        var doacao = NovaDoacao(Guid.NewGuid(), 10.00m);
        await using var provedor = ambiente.CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);

        (await harness.Published.Any<Fault<DoacaoRecebidaEvent>>(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        harness.Consumed.Select<DoacaoRecebidaEvent>(TestContext.Current.CancellationToken).Count().ShouldBe(1);
        harness.Consumed.Select<DoacaoProcessadaEvent>(TestContext.Current.CancellationToken).ShouldBeEmpty();
        (await ambiente.LerLivroAsync(doacao.CampanhaId)).ShouldBeEmpty();
    }

    /// <summary>
    /// Dadas vinte doações de valores diferentes consumidas ao mesmo tempo, quando todas terminam, então a soma dos
    /// lançamentos do livro é igual ao total da campanha.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consumir_VariasDoacoes_SomaDoLivroIgualAoTotal()
    {
        var campanhaId = await ambiente.CriarCampanhaAsync();
        var doacoes = Enumerable.Range(1, 20).Select(i => NovaDoacao(campanhaId, i * 1.15m)).ToList();
        await using var provedor = ambiente.CriarProvedor();
        var harness = provedor.GetRequiredService<ITestHarness>();
        await harness.Start();

        foreach (var doacao in doacoes)
        {
            await harness.Bus.Publish(doacao, TestContext.Current.CancellationToken);
        }

        await EsperarProcessadasAsync(harness, doacoes.Count);

        var total = await ambiente.LerTotalAsync(campanhaId);
        var livro = await ambiente.LerLivroAsync(campanhaId);
        livro.Count.ShouldBe(20, $"lançamentos no livro: {livro.Count}; total: {total}; consumos: {harness.Consumed.Select<DoacaoRecebidaEvent>(TestContext.Current.CancellationToken).Count()}; falhas: {harness.Consumed.Select<DoacaoRecebidaEvent>(m => m.Exception is not null, TestContext.Current.CancellationToken).Count()}");
        livro.Sum(d => d["valor"].ToDecimal()).ShouldBe(total);
        total.ShouldBe(doacoes.Sum(d => d.Valor));
    }

    /// <summary>
    /// Espera o consumo de uma quantidade de mensagens, com a conferência de que nenhuma ficou pendente.
    /// </summary>
    /// <param name="harness">Test Harness do Worker.</param>
    /// <param name="quantidade">Quantidade de consumos esperada.</param>
    /// <returns>Tarefa que termina quando os consumos ocorreram.</returns>
    private static async Task EsperarConsumoAsync(ITestHarness harness, int quantidade)
    {
        var limite = DateTime.UtcNow.AddSeconds(60);
        while (harness.Consumed.Select<DoacaoRecebidaEvent>(TestContext.Current.CancellationToken).Count() < quantidade && DateTime.UtcNow < limite)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        await harness.InactivityTask;
    }

    /// <summary>
    /// Espera a entrega de uma quantidade de <see cref="DoacaoProcessadaEvent"/>, que só acontece depois do commit da transação
    /// do consumo; é a barreira certa para ler os bancos sem disputar com consumos em andamento.
    /// </summary>
    /// <param name="harness">Test Harness do Worker.</param>
    /// <param name="quantidade">Quantidade de eventos esperada.</param>
    /// <returns>Tarefa que termina quando os eventos foram entregues ou o tempo acabou.</returns>
    private static async Task EsperarProcessadasAsync(ITestHarness harness, int quantidade)
    {
        var limite = DateTime.UtcNow.AddSeconds(60);
        while (harness.Consumed.Select<DoacaoProcessadaEvent>(TestContext.Current.CancellationToken).Count() < quantidade && DateTime.UtcNow < limite)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Cria uma doação recebida com identificadores novos.
    /// </summary>
    /// <param name="campanhaId">Campanha da doação.</param>
    /// <param name="valor">Valor da doação.</param>
    /// <returns>O evento de doação recebida.</returns>
    private static DoacaoRecebidaEvent NovaDoacao(Guid campanhaId, decimal valor)
    {
        return new DoacaoRecebidaEvent(Guid.NewGuid(), campanhaId, Guid.NewGuid(), valor, _recebida);
    }

    /// <summary>
    /// Livro que falha as primeiras gravações com erro transitório e depois grava no MongoDB real.
    /// </summary>
    /// <param name="falhas">Quantidade de gravações que falham antes de funcionar.</param>
    private sealed class LivroInstavel(int falhas) : ILivroDoacoes
    {
        /// <summary>
        /// Quantidade de gravações já tentadas.
        /// </summary>
        private int _tentativas;

        /// <summary>
        /// Obtém a quantidade de gravações tentadas até agora.
        /// </summary>
        public int Tentativas => Volatile.Read(ref _tentativas);

        /// <summary>
        /// Livro real, definido pelo contêiner na primeira chamada.
        /// </summary>
        public LivroDoacoesMongo? Real { get; set; }

        /// <inheritdoc/>
        public Task RegistrarAsync(LancamentoDoacao lancamento, CancellationToken cancelamento)
        {
            return Interlocked.Increment(ref _tentativas) <= falhas
                ? Task.FromException(new TimeoutException("MongoDB indisponível (simulado)."))
                : Real!.RegistrarAsync(lancamento, cancelamento);
        }
    }
}
