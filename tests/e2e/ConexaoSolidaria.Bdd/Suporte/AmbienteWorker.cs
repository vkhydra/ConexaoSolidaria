extern alias WorkerApp;

using System.Text.Json;
using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Arrecadacao;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Ambiente de um cenário para o Arrecadacao.Worker: o consumidor de produção (com Outbox do EF Core e livro no MongoDB)
/// sobre o Test Harness do MassTransit, ligado ao mesmo banco e ao mesmo Redis da Campanhas.Api do cenário. Nos cenários o
/// RabbitMQ é substituído por uma ponte: as doações gravadas no Outbox da API são entregues ao consumidor por
/// <see cref="EntregarPendentesAsync"/>.
/// </summary>
/// <param name="campanhas">Ambiente da Campanhas.Api, de onde vêm o banco e o Outbox.</param>
public sealed class AmbienteWorker(AmbienteCampanhas campanhas) : IAsyncDisposable
{
    /// <summary>
    /// Opções de leitura do corpo do Outbox: o MassTransit grava <c>decimal</c> como texto.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web) { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };

    /// <summary>
    /// Doações já entregues ao consumidor, para a ponte não entregar a mesma duas vezes.
    /// </summary>
    private readonly HashSet<Guid> _entregues = [];

    /// <summary>
    /// Banco do MongoDB exclusivo do cenário.
    /// </summary>
    private readonly IMongoDatabase _livro = new MongoClient(ServidorMongo.Conexao).GetDatabase($"arrecadacao_{Guid.NewGuid():N}");

    /// <summary>
    /// Envelopes dos <c>DoacaoProcessadaEvent</c> entregues pelo Outbox do Worker, como a Notificacoes.Function os receberia.
    /// </summary>
    private readonly ColetaDeProcessadas _coleta = new();

    /// <summary>
    /// Provedor de serviços do Worker, criado na primeira entrega.
    /// </summary>
    private ServiceProvider? _provedor;

    /// <summary>
    /// Test Harness do Worker.
    /// </summary>
    private ITestHarness? _harness;

    /// <summary>
    /// Entrega ao consumidor as doações gravadas no Outbox da API e ainda não entregues, e espera o processamento delas.
    /// </summary>
    /// <returns>Tarefa que termina com as doações processadas.</returns>
    public async Task EntregarPendentesAsync()
    {
        var pendentes = (await campanhas.LerDoacoesDoOutboxAsync()).Where(d => !_entregues.Contains(d.DoacaoId)).ToList();
        foreach (var doacao in pendentes)
        {
            await EntregarAsync(doacao);
            _entregues.Add(doacao.DoacaoId);
        }
    }

    /// <summary>
    /// Entrega uma doação ao consumidor, como o RabbitMQ faria, e espera o processamento terminar (o
    /// <c>DoacaoProcessadaEvent</c> sai do Outbox só depois do commit).
    /// </summary>
    /// <param name="doacao">Doação recebida.</param>
    /// <returns>Tarefa que termina quando o consumidor concluiu o processamento.</returns>
    public async Task EntregarAsync(DoacaoRecebidaEvent doacao)
    {
        ArgumentNullException.ThrowIfNull(doacao);

        var harness = await ObterHarnessAsync();
        var antes = harness.Consumed.Select<DoacaoRecebidaEvent>().Count();
        await harness.Bus.Publish(doacao);
        var limite = DateTime.UtcNow.AddSeconds(30);
        while (harness.Consumed.Select<DoacaoRecebidaEvent>().Count() <= antes && DateTime.UtcNow < limite)
        {
            await Task.Delay(25);
        }

        await harness.InactivityTask;
    }

    /// <summary>
    /// Espera o Outbox do Worker entregar o <c>DoacaoProcessadaEvent</c> da doação e devolve o envelope JSON do MassTransit,
    /// exatamente como a fila <c>notificacoes-recibo</c> o entregaria à função.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <returns>O corpo do envelope.</returns>
    public async Task<string> ObterEnvelopeProcessadoAsync(Guid doacaoId)
    {
        var harness = await ObterHarnessAsync();
        using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        (await harness.Consumed.Any<DoacaoProcessadaEvent>(e => e.Context.Message.DoacaoId == doacaoId, prazo.Token)).ShouldBeTrue();
        return _coleta.Envelopes[doacaoId];
    }

    /// <summary>
    /// Lê os lançamentos do livro de doações de uma campanha.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>Os documentos do livro daquela campanha.</returns>
    public async Task<IReadOnlyList<BsonDocument>> LerLivroAsync(Guid campanhaId)
    {
        return await _livro.GetCollection<BsonDocument>(LivroDoacoesMongo.NomeColecao)
            .Find(Builders<BsonDocument>.Filter.Eq("campanhaId", campanhaId.ToString()))
            .ToListAsync();
    }

    /// <summary>
    /// Para o Test Harness e libera o provedor.
    /// </summary>
    /// <returns>Tarefa que termina com tudo liberado.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_provedor is not null)
        {
            await _provedor.DisposeAsync();
        }
    }

    /// <summary>
    /// Interpreta o corpo de uma mensagem do Outbox da API como doação recebida.
    /// </summary>
    /// <param name="corpo">Corpo JSON da linha de <c>outbox_message</c>.</param>
    /// <returns>A doação, ou nulo se a mensagem não é de doação.</returns>
    public static DoacaoRecebidaEvent? LerDoacao(string corpo)
    {
        using var documento = JsonDocument.Parse(corpo);
        var mensagem = documento.RootElement.GetProperty("message");
        return mensagem.TryGetProperty("doacaoId", out _) ? mensagem.Deserialize<DoacaoRecebidaEvent>(_opcoes) : null;
    }

    /// <summary>
    /// Obtém o Test Harness do Worker, montando e iniciando tudo na primeira chamada.
    /// </summary>
    /// <returns>O harness já iniciado.</returns>
    private async Task<ITestHarness> ObterHarnessAsync()
    {
        if (_harness is not null)
        {
            return _harness;
        }

        await campanhas.GarantirIniciadaAsync();
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Redis"] = ServidorRedis.Conexao })
            .Build();
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddDbContext<CampanhasDbContext>(opcoes => CampanhasDbContext.Configurar(opcoes, campanhas.Conexao));
        servicos.AddSingleton(_livro);
        servicos.AddSingleton<ILivroDoacoes, LivroDoacoesMongo>();
        servicos.AddScoped<IRegistroArrecadacao, RegistroArrecadacaoSql>();
        servicos.AddSingleton<IRelogio, RelogioSaoPaulo>();
        servicos.AddCampanhasCache(configuracao);
        servicos.AddSingleton<InvalidadorDoPainel>();
        servicos.AddSingleton<IMetricasArrecadacao, MetricasDoacoesPrometheus>();
        servicos.AddSingleton(new PoliticaDeRetentativa(2, [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)]));
        servicos.AddScoped<ProcessarDoacao>();
        servicos.AddSingleton(_coleta);
        servicos.AddMassTransitTestHarness(registro =>
        {
            ConfiguracaoMassTransit.ConfigurarOutbox(registro);
            registro.AddConsumer<WorkerApp::Arrecadacao.Worker.Consumidores.DoacaoRecebidaConsumer, WorkerApp::Arrecadacao.Worker.Consumidores.DoacaoRecebidaConsumerDefinition>();
            registro.AddConsumer<ColetorDeProcessadas>();
            registro.UsingInMemory((contexto, bus) =>
            {
                bus.UseDelayedMessageScheduler();
                bus.ConfigureEndpoints(contexto);
            });
        });
        _provedor = servicos.BuildServiceProvider(true);
        _harness = _provedor.GetRequiredService<ITestHarness>();
        await _harness.Start();
        return _harness;
    }
}
