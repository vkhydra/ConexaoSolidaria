using Campanhas.Application.Arrecadacao;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure.Arrecadacao;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Arrecadacao.Worker.IntegrationTests.Suporte;

/// <summary>
/// Sobe um PostgreSQL real em container, aplica as migrations de Campanhas e monta o registro de arrecadação com o Outbox
/// do MassTransit (sem iniciar o bus, então as mensagens ficam em <c>outbox_message</c> para os testes conferirem).
/// </summary>
public sealed class BancoWorkerFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do PostgreSQL.
    /// </summary>
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Provedor de serviços com o contexto, o Outbox e o registro de arrecadação.
    /// </summary>
    private ServiceProvider? _provedor;

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
        servicos.AddScoped<IRegistroArrecadacao, RegistroArrecadacaoSql>();
        _provedor = servicos.BuildServiceProvider();

        await using var escopo = _provedor.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Cria um escopo novo, com contexto e registro próprios (cada escopo é uma "mensagem consumida").
    /// </summary>
    /// <returns>Escopo que o chamador deve descartar.</returns>
    public AsyncServiceScope CriarEscopo()
    {
        return _provedor!.CreateAsyncScope();
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
        await using var escopo = CriarEscopo();
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
        return await ConsultarAsync<decimal>("SELECT total_arrecadado FROM campanhas WHERE id = @id", ("id", campanhaId));
    }

    /// <summary>
    /// Conta as doações processadas de uma campanha.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>A quantidade de linhas em <c>doacoes_processadas</c>.</returns>
    public async Task<long> ContarDoacoesAsync(Guid campanhaId)
    {
        return await ConsultarAsync<long>("SELECT count(*) FROM doacoes_processadas WHERE campanha_id = @id", ("id", campanhaId));
    }

    /// <summary>
    /// Lê os corpos das mensagens do Outbox que citam o identificador informado.
    /// </summary>
    /// <param name="identificador">Identificador (campanha ou doação) a procurar no corpo.</param>
    /// <returns>Os corpos (JSON) das mensagens, na ordem de gravação.</returns>
    public async Task<IReadOnlyList<string>> LerOutboxAsync(Guid identificador)
    {
        await using var conexao = new NpgsqlConnection(_container.GetConnectionString());
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

    /// <summary>
    /// Executa uma consulta escalar com um parâmetro.
    /// </summary>
    /// <typeparam name="T">Tipo do valor devolvido.</typeparam>
    /// <param name="sql">Consulta SQL.</param>
    /// <param name="parametro">Nome e valor do parâmetro.</param>
    /// <returns>O valor da primeira coluna da primeira linha.</returns>
    private async Task<T> ConsultarAsync<T>(string sql, (string Nome, object Valor) parametro)
    {
        await using var conexao = new NpgsqlConnection(_container.GetConnectionString());
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        comando.Parameters.AddWithValue(parametro.Nome, parametro.Valor);
        return (T)(await comando.ExecuteScalarAsync())!;
    }
}
