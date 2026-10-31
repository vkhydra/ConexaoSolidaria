using Npgsql;
using Reqnroll;
using Testcontainers.PostgreSql;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// PostgreSQL em container, compartilhado por todos os cenários da execução. Cada cenário recebe um banco próprio.
/// </summary>
[Binding]
public static class ServidorPostgres
{
    /// <summary>
    /// Container do PostgreSQL, criado antes do primeiro cenário.
    /// </summary>
    private static PostgreSqlContainer? _container;

    /// <summary>
    /// Sobe o container uma única vez, antes de todos os cenários.
    /// </summary>
    /// <returns>Tarefa que termina com o servidor pronto.</returns>
    [BeforeTestRun]
    public static async Task IniciarAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
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

    /// <summary>
    /// Cria um banco vazio com nome único.
    /// </summary>
    /// <returns>String de conexão do banco criado.</returns>
    public static async Task<string> CriarBancoAsync()
    {
        var administrador = _container?.GetConnectionString()
            ?? throw new InvalidOperationException("O servidor PostgreSQL não foi iniciado.");
        var nome = $"bdd_{Guid.NewGuid():N}";

        await using (var conexao = new NpgsqlConnection(administrador))
        {
            await conexao.OpenAsync();
            await using var comando = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(administrador) { Database = nome }.ConnectionString;
    }
}
