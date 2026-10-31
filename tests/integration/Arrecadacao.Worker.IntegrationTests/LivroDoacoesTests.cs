using Arrecadacao.Worker.IntegrationTests.Suporte;
using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure.Arrecadacao;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Arrecadacao.Worker.IntegrationTests;

/// <summary>
/// Testes do livro de doações contra um MongoDB real: documento, tipos monetários, idempotência do upsert e índice.
/// </summary>
/// <param name="mongo">MongoDB compartilhado pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class LivroDoacoesTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    /// <summary>
    /// Instante em que a doação foi aceita, nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _recebida = new(2026, 11, 10, 14, 10, 5, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dado um lançamento novo, quando registrado, então o documento tem o id da doação, valores Decimal128, datas UTC e versão de esquema.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_LancamentoNovo_GravaODocumentoDoContrato()
    {
        var lancamento = NovoLancamento(150.00m, 1150.00m);

        await new LivroDoacoesMongo(mongo.Banco).RegistrarAsync(lancamento, TestContext.Current.CancellationToken);

        var documento = await mongo.LerLancamentoAsync(lancamento.DoacaoId, LivroDoacoesMongo.NomeColecao);
        documento.ShouldNotBeNull();
        documento["_id"].AsString.ShouldBe(lancamento.DoacaoId.ToString());
        documento["campanhaId"].AsString.ShouldBe(lancamento.CampanhaId.ToString());
        documento["doadorId"].AsString.ShouldBe(lancamento.DoadorId.ToString());
        documento["valor"].BsonType.ShouldBe(BsonType.Decimal128);
        documento["valor"].ToDecimal().ShouldBe(150.00m);
        documento["totalAposLancamento"].ToDecimal().ShouldBe(1150.00m);
        documento["recebidaEm"].ToUniversalTime().ShouldBe(_recebida.UtcDateTime);
        documento["processadaEm"].ToUniversalTime().ShouldBe(_recebida.AddSeconds(1).UtcDateTime);
        documento["versaoEsquema"].AsInt32.ShouldBe(1);
        documento.Contains("traceId").ShouldBeTrue();
    }

    /// <summary>
    /// Dado um lançamento gravado dentro de uma atividade de trace, quando lido, então o traceId do documento é o da atividade;
    /// sem atividade, o campo é nulo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_ComESemAtividade_GravaOTraceIdOuNulo()
    {
        var livro = new LivroDoacoesMongo(mongo.Banco);
        var semTrace = NovoLancamento(5.00m, 5.00m);
        var comTrace = NovoLancamento(6.00m, 11.00m);
        await livro.RegistrarAsync(semTrace, TestContext.Current.CancellationToken);
        using var fonte = new System.Diagnostics.ActivitySource("teste.livro");
        using var ouvinte = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = origem => origem.Name == "teste.livro",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
        };
        System.Diagnostics.ActivitySource.AddActivityListener(ouvinte);
        using var atividade = fonte.StartActivity("gravar");

        await livro.RegistrarAsync(comTrace, TestContext.Current.CancellationToken);

        (await mongo.LerLancamentoAsync(semTrace.DoacaoId, LivroDoacoesMongo.NomeColecao))!["traceId"].IsBsonNull.ShouldBeTrue();
        (await mongo.LerLancamentoAsync(comTrace.DoacaoId, LivroDoacoesMongo.NomeColecao))!["traceId"].AsString.ShouldBe(atividade!.TraceId.ToString());
    }

    /// <summary>
    /// Dado um lançamento já gravado, quando o mesmo é registrado de novo com outro total, então o documento original não muda
    /// e continua havendo um só.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_Reentrega_NaoAlteraOLancamentoExistente()
    {
        var livro = new LivroDoacoesMongo(mongo.Banco);
        var original = NovoLancamento(150.00m, 1150.00m);
        await livro.RegistrarAsync(original, TestContext.Current.CancellationToken);

        await livro.RegistrarAsync(original with { TotalAposLancamento = 9999.00m, ProcessadaEm = original.ProcessadaEm.AddMinutes(30) }, TestContext.Current.CancellationToken);

        var colecao = mongo.Banco.GetCollection<BsonDocument>(LivroDoacoesMongo.NomeColecao);
        (await colecao.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", original.DoacaoId.ToString()), cancellationToken: TestContext.Current.CancellationToken)).ShouldBe(1);
        var documento = await mongo.LerLancamentoAsync(original.DoacaoId, LivroDoacoesMongo.NomeColecao);
        documento!["totalAposLancamento"].ToDecimal().ShouldBe(1150.00m);
        documento["processadaEm"].ToUniversalTime().ShouldBe(original.ProcessadaEm.UtcDateTime);
    }

    /// <summary>
    /// Dado o mesmo lançamento gravado ao mesmo tempo por vários consumidores, quando todos terminam, então há um só documento.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_MesmoLancamentoEmParalelo_GravaUmUnicoDocumento()
    {
        var lancamento = NovoLancamento(10.00m, 10.00m);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => new LivroDoacoesMongo(mongo.Banco).RegistrarAsync(lancamento, TestContext.Current.CancellationToken)));

        var colecao = mongo.Banco.GetCollection<BsonDocument>(LivroDoacoesMongo.NomeColecao);
        (await colecao.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", lancamento.DoacaoId.ToString()), cancellationToken: TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    /// <summary>
    /// Dado o livro em uso, quando os índices são listados, então existe o índice por campanha e data de processamento.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_PrimeiraGravacao_CriaOIndicePorCampanha()
    {
        await new LivroDoacoesMongo(mongo.Banco).RegistrarAsync(NovoLancamento(1.00m, 1.00m), TestContext.Current.CancellationToken);

        var indices = await (await mongo.Banco.GetCollection<BsonDocument>(LivroDoacoesMongo.NomeColecao).Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);

        var indice = indices.Single(i => i["name"].AsString == "ix_campanha_processada_em");
        indice["key"].AsBsonDocument.ToJson().ShouldBe("{ \"campanhaId\" : 1, \"processadaEm\" : -1 }");
    }

    /// <summary>
    /// Dado um lançamento nulo, quando registrado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Registrar_LancamentoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => new LivroDoacoesMongo(mongo.Banco).RegistrarAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Cria um lançamento com identificadores novos.
    /// </summary>
    /// <param name="valor">Valor doado.</param>
    /// <param name="total">Total da campanha depois do lançamento.</param>
    /// <returns>O lançamento.</returns>
    private static LancamentoDoacao NovoLancamento(decimal valor, decimal total)
    {
        return new LancamentoDoacao(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), valor, total, _recebida, _recebida.AddSeconds(1));
    }
}
