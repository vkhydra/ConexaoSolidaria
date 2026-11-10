using Arrecadacao.Worker.Consumidores;
using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Transparencia;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Arrecadacao;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace Arrecadacao.Worker.IntegrationTests.Suporte;

/// <summary>
/// Sobe PostgreSQL e MongoDB reais em containers e monta o Worker inteiro (consumidor com a definição de produção, Outbox do
/// EF Core, livro no MongoDB) sobre o Test Harness do MassTransit, com transporte em memória e intervalos de retentativa curtos.
/// </summary>
public sealed class ConsumidorFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do PostgreSQL.
    /// </summary>
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Container do MongoDB.
    /// </summary>
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:7.0").Build();

    /// <summary>
    /// Banco do MongoDB usado pelos testes, com nome único por fixture.
    /// </summary>
    private IMongoDatabase _banco = null!;

    /// <summary>
    /// Obtém a string de conexão do PostgreSQL do container.
    /// </summary>
    public string StringConexaoPostgres => _postgres.GetConnectionString();

    /// <summary>
    /// Sobe os containers e aplica as migrations de Campanhas.
    /// </summary>
    /// <returns>Tarefa que termina com os bancos prontos.</returns>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _mongo.StartAsync());
        _banco = new MongoClient(_mongo.GetConnectionString()).GetDatabase($"arrecadacao_{Guid.NewGuid():N}");

        await using var provedor = CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Monta um Worker completo sobre o Test Harness, ainda parado.
    /// </summary>
    /// <param name="ajustar">Ajuste opcional do contêiner, por exemplo para trocar o livro por um que falha.</param>
    /// <returns>O provedor de serviços; o chamador obtém o <see cref="ITestHarness"/> dele e o inicia.</returns>
    public ServiceProvider CriarProvedor(Action<IServiceCollection>? ajustar = null)
    {
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddDbContext<CampanhasDbContext>(opcoes => CampanhasDbContext.Configurar(opcoes, _postgres.GetConnectionString()));
        servicos.AddSingleton(_banco);
        servicos.AddSingleton<ILivroDoacoes, LivroDoacoesMongo>();
        servicos.AddScoped<IRegistroArrecadacao, RegistroArrecadacaoSql>();
        servicos.AddSingleton<IRelogio, RelogioSaoPaulo>();
        servicos.AddSingleton(Substitute.For<ICachePainel>());
        servicos.AddSingleton<InvalidadorDoPainel>();
        servicos.AddSingleton(Substitute.For<IMetricasArrecadacao>());
        servicos.AddSingleton(new PoliticaDeRetentativa(2, [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)]));
        servicos.AddScoped<ProcessarDoacao>();
        ajustar?.Invoke(servicos);
        servicos.AddMassTransitTestHarness(registro =>
        {
            ConfiguracaoMassTransit.ConfigurarOutbox(registro);
            registro.AddConsumer<DoacaoRecebidaConsumer, DoacaoRecebidaConsumerDefinition>();
            registro.AddConsumer<EspiaoDeProcessadas>();
            registro.UsingInMemory((contexto, bus) =>
            {
                bus.UseDelayedMessageScheduler();
                bus.ConfigureEndpoints(contexto);
            });
        });
        return servicos.BuildServiceProvider(true);
    }

    /// <summary>
    /// Grava uma campanha ativa de 10000.00 e devolve o identificador.
    /// </summary>
    /// <returns>O identificador da campanha criada.</returns>
    public async Task<Guid> CriarCampanhaAsync()
    {
        var hoje = new DateOnly(2026, 11, 10);
        var campanha = Campanha.Criar(
            "Natal Solidário",
            "Cestas",
            Periodo.Criar(hoje, hoje.AddDays(30)).Valor,
            Dinheiro.Criar(10000m).Valor,
            hoje,
            new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3))).Valor;
        await using var provedor = CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>();
        contexto.Campanhas.Add(campanha);
        await contexto.SaveChangesAsync();
        return campanha.Id.Valor;
    }

    /// <summary>
    /// Lê o total arrecadado de uma campanha direto do banco.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>O total arrecadado.</returns>
    public async Task<decimal> LerTotalAsync(Guid campanhaId)
    {
        await using var conexao = new NpgsqlConnection(_postgres.GetConnectionString());
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT total_arrecadado FROM campanhas WHERE id = @id", conexao);
        comando.Parameters.AddWithValue("id", campanhaId);
        return (decimal)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Registra a doação direto no PostgreSQL, sem o livro, como se a primeira tentativa tivesse caído depois do COMMIT.
    /// </summary>
    /// <param name="doacao">Doação a registrar.</param>
    /// <returns>Tarefa que termina com a doação registrada.</returns>
    public async Task RegistrarSemLivroAsync(ConexaoSolidaria.Contracts.V1.DoacaoRecebidaEvent doacao)
    {
        await using var provedor = CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IRegistroArrecadacao>().RegistrarAsync(doacao, DateTimeOffset.UtcNow, CancellationToken.None);
    }

    /// <summary>
    /// Lê os lançamentos do livro de uma campanha.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>Os documentos do livro daquela campanha.</returns>
    public async Task<IReadOnlyList<BsonDocument>> LerLivroAsync(Guid campanhaId)
    {
        return await _banco.GetCollection<BsonDocument>(LivroDoacoesMongo.NomeColecao)
            .Find(Builders<BsonDocument>.Filter.Eq("campanhaId", campanhaId.ToString()))
            .ToListAsync();
    }

    /// <summary>
    /// Lê os corpos das mensagens do Outbox que citam o identificador informado.
    /// </summary>
    /// <param name="identificador">Identificador (campanha ou doação) a procurar no corpo.</param>
    /// <returns>Os corpos (JSON) das mensagens, na ordem de gravação.</returns>
    public async Task<IReadOnlyList<string>> LerOutboxAsync(Guid identificador)
    {
        await using var conexao = new NpgsqlConnection(_postgres.GetConnectionString());
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT body FROM outbox_message WHERE body LIKE @id ORDER BY sequence_number", conexao);
        comando.Parameters.AddWithValue("id", $"%{identificador}%");
        await using var leitor = await comando.ExecuteReaderAsync();
        var corpos = new List<string>();
        while (await leitor.ReadAsync())
        {
            corpos.Add(leitor.GetString(0));
        }

        return corpos;
    }

    /// <summary>
    /// Remove os containers.
    /// </summary>
    /// <returns>Tarefa que termina com tudo removido.</returns>
    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _mongo.DisposeAsync();
    }
}
