using System.Text.Json;
using Arrecadacao.Worker.IntegrationTests.Suporte;
using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.DependencyInjection;

namespace Arrecadacao.Worker.IntegrationTests;

/// <summary>
/// Testes do registro de arrecadação contra um PostgreSQL real: soma atômica, idempotência por doação, campanha inexistente,
/// concorrência e Outbox transacional.
/// </summary>
/// <param name="banco">Banco compartilhado pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class RegistroArrecadacaoTests(BancoWorkerFixture banco) : IClassFixture<BancoWorkerFixture>
{
    /// <summary>
    /// Instante do processamento nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 10, 6, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dada uma doação nova, quando registrada, então o total sobe, o registro de idempotência é gravado e o
    /// DoacaoProcessadaEvent fica no Outbox com o total novo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_DoacaoNova_SomaEPublicaEventoNoOutbox()
    {
        var campanhaId = await banco.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 150.00m);

        var resultado = await RegistrarAsync(doacao);

        resultado.Situacao.ShouldBe(SituacaoRegistro.Registrada);
        resultado.TotalArrecadado.ShouldBe(150.00m);
        resultado.ProcessadaEm.ShouldBe(_agora);
        (await banco.LerTotalAsync(campanhaId)).ShouldBe(150.00m);
        (await banco.ContarDoacoesAsync(campanhaId)).ShouldBe(1);
        var evento = JsonDocument.Parse((await banco.LerOutboxAsync(doacao.DoacaoId)).ShouldHaveSingleItem()).RootElement.GetProperty("message");
        evento.GetProperty("doacaoId").GetGuid().ShouldBe(doacao.DoacaoId);
        evento.GetProperty("doadorId").GetGuid().ShouldBe(doacao.DoadorId);
        decimal.Parse(evento.GetProperty("totalArrecadado").GetString()!, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(150.00m);
    }

    /// <summary>
    /// Dadas duas doações da mesma campanha, quando registradas, então o total acumula e cada uma publica o seu evento.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_DuasDoacoes_AcumulaOTotal()
    {
        var campanhaId = await banco.CriarCampanhaAsync();

        await RegistrarAsync(NovaDoacao(campanhaId, 150.00m));
        var segunda = await RegistrarAsync(NovaDoacao(campanhaId, 49.90m));

        segunda.TotalArrecadado.ShouldBe(199.90m);
        (await banco.LerTotalAsync(campanhaId)).ShouldBe(199.90m);
        (await banco.ContarDoacoesAsync(campanhaId)).ShouldBe(2);
        (await banco.LerOutboxAsync(campanhaId)).Count(m => m.Contains("totalArrecadado", StringComparison.Ordinal)).ShouldBe(2);
    }

    /// <summary>
    /// Dada uma doação já somada, quando a mesma mensagem chega de novo, então o total não muda, o instante é o da primeira vez
    /// e nenhum evento novo é publicado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_Reentrega_NaoSomaDeNovo()
    {
        var campanhaId = await banco.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 150.00m);
        await RegistrarAsync(doacao);

        var reentrega = await RegistrarAsync(doacao, _agora.AddMinutes(5));

        reentrega.Situacao.ShouldBe(SituacaoRegistro.JaProcessada);
        reentrega.ProcessadaEm.ShouldBe(_agora);
        reentrega.TotalArrecadado.ShouldBe(150.00m);
        (await banco.LerTotalAsync(campanhaId)).ShouldBe(150.00m);
        (await banco.ContarDoacoesAsync(campanhaId)).ShouldBe(1);
        (await banco.LerOutboxAsync(doacao.DoacaoId)).ShouldHaveSingleItem();
    }

    /// <summary>
    /// Dada uma campanha que não existe, quando a doação é registrada, então nada é gravado e nenhum evento é publicado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_CampanhaInexistente_NaoGravaNada()
    {
        var doacao = NovaDoacao(Guid.NewGuid(), 10.00m);

        var resultado = await RegistrarAsync(doacao);

        resultado.Situacao.ShouldBe(SituacaoRegistro.CampanhaInexistente);
        resultado.TotalArrecadado.ShouldBeNull();
        (await banco.LerOutboxAsync(doacao.DoacaoId)).ShouldBeEmpty();
    }

    /// <summary>
    /// Dada a mesma doação entregue ao mesmo tempo a vários consumidores, quando registrada, então só uma soma acontece.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_MesmaDoacaoEmParalelo_SomaUmaUnicaVez()
    {
        var campanhaId = await banco.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 80.00m);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => RegistrarAsync(doacao)));

        resultados.Count(r => r.Situacao == SituacaoRegistro.Registrada).ShouldBe(1);
        resultados.Count(r => r.Situacao == SituacaoRegistro.JaProcessada).ShouldBe(5);
        (await banco.LerTotalAsync(campanhaId)).ShouldBe(80.00m);
        (await banco.LerOutboxAsync(doacao.DoacaoId)).ShouldHaveSingleItem();
    }

    /// <summary>
    /// Dadas vinte doações diferentes registradas ao mesmo tempo, quando todas terminam, então o total é a soma exata
    /// (nenhuma atualização se perde).
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_DoacoesDiferentesEmParalelo_NaoPerdeNenhumaSoma()
    {
        var campanhaId = await banco.CriarCampanhaAsync();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => RegistrarAsync(NovaDoacao(campanhaId, 10.00m))));

        (await banco.LerTotalAsync(campanhaId)).ShouldBe(200.00m);
        (await banco.ContarDoacoesAsync(campanhaId)).ShouldBe(20);
    }

    /// <summary>
    /// Dada uma transação já aberta no contexto (a do Outbox do consumidor), quando a doação é registrada e repetida na mesma
    /// transação, então a repetição não aborta a transação e, ao confirmá-la, só uma soma vale.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_EmTransacaoExterna_ReaproveitaATransacaoERepeticaoNaoAborta()
    {
        var campanhaId = await banco.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 30.00m);
        await using (var escopo = banco.CriarEscopo())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>();
            var registro = escopo.ServiceProvider.GetRequiredService<IRegistroArrecadacao>();
            await using var transacao = await contexto.Database.BeginTransactionAsync(CancellationToken.None);

            var primeira = await registro.RegistrarAsync(doacao, _agora, CancellationToken.None);
            var repetida = await registro.RegistrarAsync(doacao, _agora, CancellationToken.None);
            (await banco.LerTotalAsync(campanhaId)).ShouldBe(0m);
            await contexto.SaveChangesAsync(CancellationToken.None);
            await transacao.CommitAsync(CancellationToken.None);

            primeira.Situacao.ShouldBe(SituacaoRegistro.Registrada);
            repetida.Situacao.ShouldBe(SituacaoRegistro.JaProcessada);
        }

        (await banco.LerTotalAsync(campanhaId)).ShouldBe(30.00m);
        (await banco.LerOutboxAsync(doacao.DoacaoId)).ShouldHaveSingleItem();
    }

    /// <summary>
    /// Dada uma transação externa desfeita, quando a doação já tinha sido registrada nela, então nada fica gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_EmTransacaoExternaDesfeita_NaoDeixaNada()
    {
        var campanhaId = await banco.CriarCampanhaAsync();
        var doacao = NovaDoacao(campanhaId, 30.00m);
        await using (var escopo = banco.CriarEscopo())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>();
            await using var transacao = await contexto.Database.BeginTransactionAsync(CancellationToken.None);
            await escopo.ServiceProvider.GetRequiredService<IRegistroArrecadacao>().RegistrarAsync(doacao, _agora, CancellationToken.None);
            await transacao.RollbackAsync(CancellationToken.None);
        }

        (await banco.LerTotalAsync(campanhaId)).ShouldBe(0m);
        (await banco.ContarDoacoesAsync(campanhaId)).ShouldBe(0);
    }

    /// <summary>
    /// Registra a doação em um escopo novo, como cada mensagem consumida faz.
    /// </summary>
    /// <param name="doacao">Doação recebida.</param>
    /// <param name="agora">Instante do processamento; o padrão é <see cref="_agora"/>.</param>
    /// <returns>O resultado do registro.</returns>
    private async Task<ResultadoRegistro> RegistrarAsync(DoacaoRecebidaEvent doacao, DateTimeOffset? agora = null)
    {
        await using var escopo = banco.CriarEscopo();
        return await escopo.ServiceProvider.GetRequiredService<IRegistroArrecadacao>().RegistrarAsync(doacao, agora ?? _agora, CancellationToken.None);
    }

    /// <summary>
    /// Cria uma doação recebida com identificadores novos.
    /// </summary>
    /// <param name="campanhaId">Campanha da doação.</param>
    /// <param name="valor">Valor da doação.</param>
    /// <returns>O evento de doação recebida.</returns>
    private static DoacaoRecebidaEvent NovaDoacao(Guid campanhaId, decimal valor)
    {
        return new DoacaoRecebidaEvent(Guid.NewGuid(), campanhaId, Guid.NewGuid(), valor, _agora.AddSeconds(-1));
    }
}
