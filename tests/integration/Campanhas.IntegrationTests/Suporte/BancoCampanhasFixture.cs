using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Campanhas.IntegrationTests.Suporte;

/// <summary>
/// Sobe um PostgreSQL real em container, aplica as migrations de Campanhas e monta o repositório com o Outbox do MassTransit
/// (sem iniciar o bus, então as mensagens ficam em <c>outbox_message</c> para os testes conferirem).
/// </summary>
public sealed class BancoCampanhasFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do PostgreSQL.
    /// </summary>
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Provedor de serviços com o contexto, o Outbox e o repositório.
    /// </summary>
    private ServiceProvider? _provedor;

    /// <summary>
    /// Obtém a string de conexão do banco do container.
    /// </summary>
    public string StringConexao => _container.GetConnectionString();

    /// <summary>
    /// Sobe o container, aplica as migrations e monta os serviços.
    /// </summary>
    /// <returns>Tarefa que termina com o banco pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddDbContext<CampanhasDbContext>(opcoes => CampanhasDbContext.Configurar(opcoes, _container.GetConnectionString()));
        servicos.AddMassTransit(registro =>
        {
            ConfiguracaoMassTransit.ConfigurarOutbox(registro);
            registro.UsingInMemory();
        });
        servicos.AddScoped<CampanhaRepositorio>();
        _provedor = servicos.BuildServiceProvider();

        await using var escopo = _provedor.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Cria um escopo novo, com contexto e repositório próprios (cada escopo é uma "requisição").
    /// </summary>
    /// <returns>Escopo que o chamador deve descartar.</returns>
    public AsyncServiceScope CriarEscopo()
    {
        return _provedor!.CreateAsyncScope();
    }

    /// <summary>
    /// Conta as mensagens do Outbox cujo corpo contém o identificador da campanha.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>Os corpos (JSON) das mensagens do Outbox daquela campanha, na ordem de gravação.</returns>
    public async Task<IReadOnlyList<string>> LerOutboxAsync(Guid campanhaId)
    {
        await using var conexao = new NpgsqlConnection(_container.GetConnectionString());
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT body FROM outbox_message WHERE body LIKE @id ORDER BY sequence_number", conexao);
        comando.Parameters.AddWithValue("id", $"%{campanhaId}%");
        await using var leitor = await comando.ExecuteReaderAsync();
        var corpos = new List<string>();
        while (await leitor.ReadAsync())
        {
            corpos.Add(leitor.GetString(0));
        }

        return corpos;
    }

    /// <summary>
    /// Remove o provedor e o container.
    /// </summary>
    /// <returns>Tarefa que termina com tudo removido.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_provedor is not null)
        {
            await _provedor.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
