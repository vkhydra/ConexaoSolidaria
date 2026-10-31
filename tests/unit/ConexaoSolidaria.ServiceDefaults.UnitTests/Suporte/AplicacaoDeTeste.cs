using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;

/// <summary>
/// Cria uma aplicação ASP.NET Core mínima, em memória, para testar os recursos do ServiceDefaults
/// sem abrir portas de rede.
/// </summary>
public static class AplicacaoDeTeste
{
    /// <summary>
    /// Cria o construtor de uma aplicação que roda no servidor de teste.
    /// </summary>
    /// <param name="ambiente">Nome do ambiente (Development, Production...).</param>
    /// <returns>Construtor já configurado para o servidor de teste.</returns>
    public static WebApplicationBuilder CriarConstrutor(string ambiente = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = ambiente });
        builder.WebHost.UseTestServer();
        return builder;
    }
}
