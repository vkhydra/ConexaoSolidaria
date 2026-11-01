using System.Net.Security;
using System.Net.Sockets;
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
    /// SHA-256 do certificado autoassinado do container, em hexadecimal.
    /// </summary>
    private string _impressaoDigital = string.Empty;

    /// <summary>
    /// Obtém o endereço do Elasticsearch, com usuário e senha do container.
    /// </summary>
    public string Endereco => _container.GetConnectionString();

    /// <summary>
    /// Obtém as configurações <c>Elasticsearch:*</c> que apontam a Campanhas.Api para o container.
    /// </summary>
    public IReadOnlyDictionary<string, string> ConfiguracaoDaApi
    {
        get
        {
            var uri = new Uri(Endereco);
            var credenciais = uri.UserInfo.Split(':', 2);
            return new Dictionary<string, string>
            {
                ["Elasticsearch:Endereco"] = uri.GetLeftPart(UriPartial.Authority),
                ["Elasticsearch:Usuario"] = credenciais[0],
                ["Elasticsearch:Senha"] = credenciais[1],
                ["Elasticsearch:ImpressaoDigital"] = _impressaoDigital,
            };
        }
    }

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
        _impressaoDigital = await LerImpressaoDigitalAsync();
        Cliente = CriarCliente();
    }

    /// <summary>
    /// Cria um cliente novo para o endereço do container.
    /// </summary>
    /// <returns>O cliente, que confia só no certificado do container (pela impressão digital).</returns>
    public ElasticsearchClient CriarCliente()
    {
        var uri = new Uri(Endereco);
        var credenciais = uri.UserInfo.Split(':', 2);
        var configuracao = new ElasticsearchClientSettings(new Uri(uri.GetLeftPart(UriPartial.Authority)))
            .Authentication(new BasicAuthentication(credenciais[0], credenciais[1]))
            .CertificateFingerprint(_impressaoDigital);
        return new ElasticsearchClient(configuracao);
    }

    /// <summary>
    /// Abre uma conexão TLS com o container só para ler o certificado e calcular o SHA-256 dele.
    /// </summary>
    /// <returns>A impressão digital em hexadecimal.</returns>
    private async Task<string> LerImpressaoDigitalAsync()
    {
        var uri = new Uri(Endereco);
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(uri.Host, uri.Port);
        string? impressao = null;
        await using var ssl = new SslStream(tcp.GetStream(), false, (_, certificado, _, _) =>
        {
            impressao = Convert.ToHexString(certificado!.GetCertHash(System.Security.Cryptography.HashAlgorithmName.SHA256));

            // Recusa o certificado: o objetivo é só lê-lo, e a conexão não é usada.
            return false;
        });
        try
        {
            await ssl.AuthenticateAsClientAsync(uri.Host);
        }
        catch (System.Security.Authentication.AuthenticationException) when (impressao is not null)
        {
            // Esperado: o certificado foi lido e recusado de propósito.
        }

        return impressao ?? throw new InvalidOperationException("O Elasticsearch não apresentou certificado.");
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
