using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Arrecadacao.Worker.UnitTests;

/// <summary>
/// Testes da partida do Arrecadacao.Worker: o host sobe, responde às sondas de saúde e expõe as métricas, sem precisar do
/// banco nem do broker (os serviços do MassTransit ficam desligados).
/// </summary>
[Trait("Category", "Unit")]
public sealed class PartidaTests : IClassFixture<PartidaTests.WorkerFactory>
{
    /// <summary>
    /// Fábrica compartilhada que sobe o Worker.
    /// </summary>
    private readonly WorkerFactory _worker;

    /// <summary>
    /// Recebe a fábrica compartilhada.
    /// </summary>
    /// <param name="worker">Fábrica do Worker.</param>
    public PartidaTests(WorkerFactory worker)
    {
        _worker = worker;
    }

    /// <summary>
    /// Dado o Worker no ar, quando a sonda de vida é chamada, então responde 200.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task SondaDeVida_WorkerNoAr_Devolve200()
    {
        using var cliente = _worker.CreateClient();

        var resposta = await cliente.GetAsync("/health/live", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Dado o Worker no ar, quando as métricas são coletadas, então o formato do Prometheus é devolvido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Metricas_WorkerNoAr_DevolveFormatoPrometheus()
    {
        using var cliente = _worker.CreateClient();

        var resposta = await cliente.GetAsync("/metrics", TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("# HELP");
    }

    /// <summary>
    /// Fábrica que sobe o Worker com configuração falsa e sem os serviços em segundo plano do MassTransit.
    /// </summary>
    public sealed class WorkerFactory : WebApplicationFactory<Program>
    {
        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Campanhas", "Host=banco.invalido.local;Database=campanhas;Username=app;Password=sem-uso");
            builder.UseSetting("ConnectionStrings:Mongo", "mongodb://mongo.invalido.local:27017");
            builder.UseSetting("RabbitMq:Host", "rabbitmq.invalido.local");
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
}
