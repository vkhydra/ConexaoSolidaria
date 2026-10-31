using System.Security.Cryptography;
using Campanhas.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Campanhas.IntegrationTests.Suporte;

/// <summary>
/// Sobe a Campanhas.Api inteira (pipeline real, PostgreSQL em container e chaves JWT geradas na hora) para os testes de HTTP.
/// Os serviços em segundo plano do MassTransit ficam desligados: o evento permanece em <c>outbox_message</c> para ser conferido.
/// </summary>
public sealed class CampanhasApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Container do PostgreSQL.
    /// </summary>
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Par de chaves RSA do JWT, gerado só para os testes.
    /// </summary>
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>
    /// Sobe o banco antes da API.
    /// </summary>
    /// <returns>Tarefa que termina com o banco pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        using var cliente = CreateClient();
        await using var escopo = Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<CampanhasDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Gera um JWT válido para a API de teste, assinado com a chave privada da fixture.
    /// </summary>
    /// <param name="role">Papel do token (<c>GestorONG</c> ou <c>Doador</c>).</param>
    /// <returns>O JWT assinado.</returns>
    public string GerarToken(string role)
    {
        var descritor = new SecurityTokenDescriptor
        {
            Issuer = "conexao-identity",
            Audience = "conexao-api",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString(), ["role"] = role },
        };

        return new JsonWebTokenHandler().CreateToken(descritor);
    }

    /// <summary>
    /// Lê os corpos das mensagens do Outbox que citam a campanha.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>Os corpos (JSON) das mensagens, na ordem de gravação.</returns>
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
    /// Libera a API, o banco e a chave.
    /// </summary>
    /// <returns>Tarefa que termina com tudo liberado.</returns>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
        _rsa.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Campanhas", _container.GetConnectionString());
        builder.UseSetting("RabbitMq:Host", "rabbitmq.invalido.local");
        builder.UseSetting("Jwt:ChavePublicaPem", _rsa.ExportSubjectPublicKeyInfoPem());
        builder.ConfigureServices(servicos =>
        {
            var emSegundoPlano = servicos
                .Where(d => d.ServiceType == typeof(IHostedService) && (d.ImplementationType?.FullName?.Contains("MassTransit", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var descritor in emSegundoPlano)
            {
                servicos.Remove(descritor);
            }
        });
    }
}
