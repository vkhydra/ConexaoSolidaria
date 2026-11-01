using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;

namespace Campanhas.IntegrationTests.Suporte;

/// <summary>
/// Sobe um Elasticsearch real em container (nó único, heap de 512 MB) para os testes de busca.
/// </summary>
public sealed class ElasticsearchFixture : IAsyncLifetime
{
    /// <summary>
    /// Container do Elasticsearch.
    /// </summary>
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.2.0")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
        .WithEnvironment("action.destructive_requires_name", "false")
        .Build();

    /// <summary>
    /// Obtém o endereço do Elasticsearch, com usuário e senha do container.
    /// </summary>
    public string Endereco => _container.GetConnectionString();

    /// <summary>
    /// Obtém um cliente apontado para o container.
    /// </summary>
    public ElasticsearchClient Cliente { get; private set; } = null!;

    /// <summary>
    /// Sobe o container e cria o cliente.
    /// </summary>
    /// <returns>Tarefa que termina com o Elasticsearch pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Cliente = CriarCliente();
    }

    /// <summary>
    /// Cria um cliente novo para o endereço do container.
    /// </summary>
    /// <returns>O cliente, que aceita o certificado do container.</returns>
    public ElasticsearchClient CriarCliente()
    {
        var uri = new Uri(Endereco);
        var credenciais = uri.UserInfo.Split(':', 2);
        var configuracao = new ElasticsearchClientSettings(new Uri(uri.GetLeftPart(UriPartial.Authority)))
            .Authentication(new BasicAuthentication(credenciais[0], credenciais[1]))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        return new ElasticsearchClient(configuracao);
    }

    /// <summary>
    /// Para o container, para simular a queda do Elasticsearch.
    /// </summary>
    /// <returns>Tarefa que termina com o container parado.</returns>
    public async Task PararAsync()
    {
        await _container.StopAsync();
    }

    /// <summary>
    /// Liga o container de novo depois de <see cref="PararAsync"/>.
    /// </summary>
    /// <returns>Tarefa que termina com o Elasticsearch no ar.</returns>
    public async Task IniciarAsync()
    {
        await _container.StartAsync();
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
