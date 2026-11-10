using Reqnroll;
using Testcontainers.MongoDb;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// MongoDB em container, compartilhado por todos os cenários da execução, para o livro de doações do Worker.
/// </summary>
[Binding]
public static class ServidorMongo
{
    /// <summary>
    /// Container do MongoDB, criado antes do primeiro cenário.
    /// </summary>
    private static MongoDbContainer? _container;

    /// <summary>
    /// Obtém a string de conexão do MongoDB do container.
    /// </summary>
    public static string Conexao => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("O servidor MongoDB não foi iniciado.");

    /// <summary>
    /// Sobe o container uma única vez, antes de todos os cenários.
    /// </summary>
    /// <returns>Tarefa que termina com o MongoDB pronto.</returns>
    [BeforeTestRun]
    public static async Task IniciarAsync()
    {
        _container = new MongoDbBuilder("mongo:7.0").Build();
        await _container.StartAsync();
    }

    /// <summary>
    /// Remove o container depois do último cenário.
    /// </summary>
    /// <returns>Tarefa que termina com o container removido.</returns>
    [AfterTestRun]
    public static async Task EncerrarAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
