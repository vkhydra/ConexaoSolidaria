using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Arrecadacao.Worker.IntegrationTests.Suporte;

/// <summary>
/// Sobe um MongoDB real em container para os testes do livro de doações.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do MongoDB.
    /// </summary>
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();

    /// <summary>
    /// Obtém a string de conexão do MongoDB do container.
    /// </summary>
    public string StringConexao => _container.GetConnectionString();

    /// <summary>
    /// Obtém o banco usado pelos testes, com nome único por fixture.
    /// </summary>
    public IMongoDatabase Banco { get; private set; } = null!;

    /// <summary>
    /// Sobe o container e abre o banco de teste.
    /// </summary>
    /// <returns>Tarefa que termina com o MongoDB pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Banco = new MongoClient(_container.GetConnectionString()).GetDatabase($"arrecadacao_{Guid.NewGuid():N}");
    }

    /// <summary>
    /// Lê o lançamento de uma doação.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="colecao">Nome da coleção do livro.</param>
    /// <returns>O documento ou nulo se não existe.</returns>
    public async Task<BsonDocument?> LerLancamentoAsync(Guid doacaoId, string colecao)
    {
        return await Banco.GetCollection<BsonDocument>(colecao).Find(Builders<BsonDocument>.Filter.Eq("_id", doacaoId.ToString())).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Remove o container.
    /// </summary>
    /// <returns>Tarefa que termina com o container removido.</returns>
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
