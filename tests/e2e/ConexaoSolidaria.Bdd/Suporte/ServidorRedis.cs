using Reqnroll;
using Testcontainers.Redis;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Redis em container, compartilhado por todos os cenários da execução, para o cache do painel.
/// </summary>
[Binding]
public static class ServidorRedis
{
    /// <summary>
    /// Container do Redis, criado antes do primeiro cenário.
    /// </summary>
    private static RedisContainer? _container;

    /// <summary>
    /// Obtém a string de conexão do Redis do container.
    /// </summary>
    public static string Conexao => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("O servidor Redis não foi iniciado.");

    /// <summary>
    /// Sobe o container uma única vez, antes de todos os cenários.
    /// </summary>
    /// <returns>Tarefa que termina com o Redis pronto.</returns>
    [BeforeTestRun]
    public static async Task IniciarAsync()
    {
        _container = new RedisBuilder("redis:7-alpine").Build();
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
