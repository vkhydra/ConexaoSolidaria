using System.Net;
using ConexaoSolidaria.ServiceDefaults.Saude;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Saude;

/// <summary>
/// Testes da <see cref="SondaDeContainer"/>, usada pelo HEALTHCHECK das imagens (que não têm <c>curl</c>).
/// </summary>
[Trait("Category", "Unit")]
public sealed class SondaDeContainerTests
{
    /// <summary>
    /// Dado o argumento <c>--healthcheck</c> entre outros, quando verificado, então a sonda é solicitada; sem ele, não.
    /// </summary>
    /// <param name="argumentos">Argumentos do processo.</param>
    /// <param name="esperado">Resultado esperado.</param>
    [Theory]
    [InlineData(new[] { "--healthcheck" }, true)]
    [InlineData(new[] { "--urls", "http://x", "--healthcheck" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--outra" }, false)]
    public void Solicitada_ReconheceOArgumento(string[] argumentos, bool esperado)
    {
        SondaDeContainer.Solicitada(argumentos).ShouldBe(esperado);
    }

    /// <summary>
    /// Dadas as variáveis de ambiente do ASP.NET, quando a porta é descoberta, então vale HTTP_PORTS, depois URLS, depois 8080.
    /// </summary>
    /// <param name="portas">Valor de <c>ASPNETCORE_HTTP_PORTS</c>.</param>
    /// <param name="urls">Valor de <c>ASPNETCORE_URLS</c>.</param>
    /// <param name="esperada">Porta esperada.</param>
    [Theory]
    [InlineData("8081", null, 8081)]
    [InlineData("8082;8083", null, 8082)]
    [InlineData(null, "http://+:9090", 9090)]
    [InlineData(null, "http://+:9090;https://+:9443", 9090)]
    [InlineData(null, "invalida", 8080)]
    [InlineData("abc", null, 8080)]
    [InlineData(null, null, 8080)]
    [InlineData("", "", 8080)]
    public void DescobrirPorta_SegueAOrdemDasVariaveis(string? portas, string? urls, int esperada)
    {
        SondaDeContainer.DescobrirPorta(portas, urls).ShouldBe(esperada);
    }

    /// <summary>
    /// Dado um servidor que responde 200, quando a sonda roda, então o código de saída é 0 e a rota chamada é <c>/health/live</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task ExecutarAsync_Resposta200_DevolveZero()
    {
        var manipulador = new ManipuladorFixo(HttpStatusCode.OK);
        using var cliente = new HttpClient(manipulador);

        var codigo = await SondaDeContainer.ExecutarAsync(8081, cliente);

        codigo.ShouldBe(0);
        manipulador.UltimaRequisicao!.RequestUri!.ToString().ShouldBe("http://localhost:8081/health/live");
    }

    /// <summary>
    /// Dado um servidor que responde 503, quando a sonda roda, então o código de saída é 1.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task ExecutarAsync_Resposta503_DevolveUm()
    {
        using var cliente = new HttpClient(new ManipuladorFixo(HttpStatusCode.ServiceUnavailable));

        (await SondaDeContainer.ExecutarAsync(8080, cliente)).ShouldBe(1);
    }

    /// <summary>
    /// Dado um servidor inalcançável, quando a sonda roda, então o código de saída é 1 e nenhuma exceção sobe.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task ExecutarAsync_ServidorInalcancavel_DevolveUm()
    {
        using var cliente = new HttpClient(new ManipuladorFixo(null));

        (await SondaDeContainer.ExecutarAsync(8080, cliente)).ShouldBe(1);
    }

    /// <summary>
    /// Dada a sonda sem servidor escutando, quando executada pelas variáveis do processo, então termina com 1.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task ExecutarAsync_SemServidor_DevolveUm()
    {
        var anterior = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", "1");

            (await SondaDeContainer.ExecutarAsync()).ShouldBe(1);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", anterior);
        }
    }

    /// <summary>
    /// Manipulador que devolve sempre o mesmo status, ou falha de conexão quando o status é nulo.
    /// </summary>
    /// <param name="status">Status devolvido; nulo simula conexão recusada.</param>
    private sealed class ManipuladorFixo(HttpStatusCode? status) : HttpMessageHandler
    {
        /// <summary>
        /// Obtém a última requisição recebida.
        /// </summary>
        public HttpRequestMessage? UltimaRequisicao { get; private set; }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaRequisicao = request;
            return status is null
                ? throw new HttpRequestException("conexão recusada")
                : Task.FromResult(new HttpResponseMessage(status.Value));
        }
    }
}
