using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Reqnroll;
using Testcontainers.Elasticsearch;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Elasticsearch em container, compartilhado pelos cenários de busca. Sobe só quando um cenário precisa (ele consome muita
/// memória) e é removido no fim da execução.
/// </summary>
[Binding]
public static class ServidorElasticsearch
{
    /// <summary>
    /// Garante que o container sobe uma única vez, mesmo com cenários em paralelo.
    /// </summary>
    private static readonly SemaphoreSlim _trava = new(1, 1);

    /// <summary>
    /// Container do Elasticsearch, criado no primeiro uso.
    /// </summary>
    private static ElasticsearchContainer? _container;

    /// <summary>
    /// Cliente apontado para o container.
    /// </summary>
    private static ElasticsearchClient? _cliente;

    /// <summary>
    /// SHA-256 do certificado autoassinado do container, em hexadecimal.
    /// </summary>
    private static string _impressaoDigital = string.Empty;

    /// <summary>
    /// Sobe o container, se ainda não subiu, e apaga os índices de campanhas, para que cada cenário comece do zero.
    /// </summary>
    /// <returns>As configurações <c>Elasticsearch:*</c> que apontam a Campanhas.Api para o container.</returns>
    public static async Task<IReadOnlyDictionary<string, string>> PrepararAsync()
    {
        await _trava.WaitAsync();
        try
        {
            if (_container is null)
            {
                var container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.2.0")
                    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                    .WithEnvironment("action.destructive_requires_name", "false")
                    .Build();
                await container.StartAsync();
                var uriInicial = new Uri(container.GetConnectionString());
                _impressaoDigital = await LerImpressaoDigitalAsync(uriInicial);
                _container = container;
                var credenciaisIniciais = uriInicial.UserInfo.Split(':', 2);
                _cliente = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(uriInicial.GetLeftPart(UriPartial.Authority)))
                    .Authentication(new BasicAuthentication(credenciaisIniciais[0], credenciaisIniciais[1]))
                    .CertificateFingerprint(_impressaoDigital));
            }

            await _cliente!.Indices.DeleteAsync(Indices.Parse("campanhas-*"));
        }
        finally
        {
            _trava.Release();
        }

        var uri = new Uri(_container.GetConnectionString());
        var credenciais = uri.UserInfo.Split(':', 2);
        return new Dictionary<string, string>
        {
            ["Elasticsearch:Endereco"] = uri.GetLeftPart(UriPartial.Authority),
            ["Elasticsearch:Usuario"] = credenciais[0],
            ["Elasticsearch:Senha"] = credenciais[1],
            ["Elasticsearch:ImpressaoDigital"] = _impressaoDigital,
        };
    }

    /// <summary>
    /// Torna pesquisáveis os documentos já gravados (o Elasticsearch só os expõe depois de um <c>refresh</c>).
    /// </summary>
    /// <returns>Tarefa que termina com o índice atualizado.</returns>
    public static async Task AtualizarIndiceAsync()
    {
        await _cliente!.Indices.RefreshAsync("campanhas");
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
    /// Abre uma conexão TLS só para ler o certificado do container e calcular o SHA-256 dele.
    /// </summary>
    /// <param name="uri">Endereço do container.</param>
    /// <returns>A impressão digital em hexadecimal.</returns>
    private static async Task<string> LerImpressaoDigitalAsync(Uri uri)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(uri.Host, uri.Port);
        string? impressao = null;
        await using var ssl = new SslStream(tcp.GetStream(), false, (_, certificado, _, _) =>
        {
            impressao = Convert.ToHexString(certificado!.GetCertHash(HashAlgorithmName.SHA256));

            // Recusa o certificado: o objetivo é só lê-lo, e a conexão não é usada.
            return false;
        });
        try
        {
            await ssl.AuthenticateAsClientAsync(uri.Host);
        }
        catch (AuthenticationException) when (impressao is not null)
        {
            // Esperado: o certificado foi lido e recusado de propósito.
        }

        return impressao ?? throw new InvalidOperationException("O Elasticsearch não apresentou certificado.");
    }
}
