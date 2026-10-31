using System.Net;
using System.Net.Http.Json;
using Identity.IntegrationTests.Suporte;
using Microsoft.AspNetCore.Hosting;

namespace Identity.IntegrationTests.Autenticacao;

/// <summary>
/// Testes do seed do gestor inicial contra a API e o banco reais. Esta classe tem banco próprio, sem outros gestores.
/// </summary>
/// <param name="api">API com banco exclusivo desta classe.</param>
[Trait("Category", "Integration")]
public sealed class SeedGestorTests(IdentityApiFixture api) : IClassFixture<IdentityApiFixture>
{
    /// <summary>
    /// Dado o seed configurado e nenhum gestor, quando a API inicia duas vezes, então cria um único gestor que consegue entrar.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Inicializacao_ComSeed_CriaUmGestorIdempotenteQueFazLogin()
    {
        const string email = "gestor.inicial@exemplo.com";
        const string senha = "SenhaInicial@2026";
        void Configurar(IWebHostBuilder builder)
        {
            builder.UseSetting("Seed:GestorEmail", email);
            builder.UseSetting("Seed:GestorSenha", senha);
        }

        using var primeira = api.WithWebHostBuilder(Configurar);
        using var cliente = primeira.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha }, TestContext.Current.CancellationToken);

        using var segunda = api.WithWebHostBuilder(Configurar);
        using var outroCliente = segunda.CreateClient();

        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.ContarGestoresAsync()).ShouldBe(1);
    }
}
