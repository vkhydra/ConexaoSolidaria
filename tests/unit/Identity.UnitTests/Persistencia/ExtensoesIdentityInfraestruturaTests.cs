using System.Security.Cryptography;
using Identity.Application.Abstracoes;
using Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.UnitTests.Persistencia;

/// <summary>
/// Testes do registro dos serviços de infraestrutura da Identity.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExtensoesIdentityInfraestruturaTests
{
    /// <summary>
    /// Chave de 32 bytes em Base64 usada para as duas chaves do CPF.
    /// </summary>
    private static readonly string _chaveCpf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Dada a configuração completa, quando os serviços são registrados, então os componentes principais são resolvidos.
    /// </summary>
    [Fact]
    public void AddIdentityInfraestrutura_ConfiguracaoCompleta_RegistraServicos()
    {
        var configuracao = CriarConfiguracao(conexao: "Host=localhost;Database=identity_db", chave: "chave", emissor: "emissor-x", audiencia: "api-x");
        var servicos = new ServiceCollection().AddSingleton(TimeProvider.System).AddLogging();

        servicos.AddIdentityInfraestrutura(configuracao);

        using var provedor = servicos.BuildServiceProvider();
        using var escopo = provedor.CreateScope();
        escopo.ServiceProvider.GetRequiredService<IUsuarioRepositorio>().ShouldNotBeNull();
        escopo.ServiceProvider.GetRequiredService<ITentativaLoginRepositorio>().ShouldNotBeNull();
        provedor.GetRequiredService<IHasherEmail>().ShouldNotBeNull();
        provedor.GetRequiredService<IMetricasLogin>().ShouldNotBeNull();
    }

    /// <summary>
    /// Dada a chave privada configurada, com ou sem emissor e audiência, quando o emissor de token é resolvido, então é criado.
    /// </summary>
    /// <param name="comEmissorEAudiencia">Se emissor e audiência vêm da configuração ou usam o padrão.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddIdentityInfraestrutura_ComChavePrivada_ResolveEmissorDeToken(bool comEmissorEAudiencia)
    {
        using var rsa = RSA.Create(2048);
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Identity"] = "Host=localhost;Database=identity_db",
            ["Autenticacao:ChaveHashEmail"] = "chave",
            ["Cpf:ChaveCifra"] = _chaveCpf,
            ["Cpf:ChaveHash"] = _chaveCpf,
            ["Jwt:ChavePrivadaPem"] = rsa.ExportRSAPrivateKeyPem(),
        };
        if (comEmissorEAudiencia)
        {
            valores["Jwt:Issuer"] = "emissor-x";
            valores["Jwt:Audience"] = "api-x";
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicos = new ServiceCollection().AddSingleton(TimeProvider.System).AddLogging();
        servicos.AddIdentityInfraestrutura(configuracao);

        using var provedor = servicos.BuildServiceProvider();

        provedor.GetRequiredService<IEmissorToken>().ShouldNotBeNull();
    }

    /// <summary>
    /// Dada a string de conexão ou a chave do HMAC ausente, quando os serviços são registrados, então a aplicação não sobe.
    /// </summary>
    /// <param name="conexao">Valor de <c>ConnectionStrings:Identity</c>.</param>
    /// <param name="chave">Valor de <c>Autenticacao:ChaveHashEmail</c>.</param>
    [Theory]
    [InlineData(null, "chave")]
    [InlineData("Host=localhost", null)]
    public void AddIdentityInfraestrutura_ConfiguracaoAusente_LancaExcecao(string? conexao, string? chave)
    {
        var configuracao = CriarConfiguracao(conexao, chave);

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddIdentityInfraestrutura(configuracao));
    }

    /// <summary>
    /// Dados argumentos nulos, quando os serviços são registrados, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void AddIdentityInfraestrutura_ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => ExtensoesIdentityInfraestrutura.AddIdentityInfraestrutura(null!, CriarConfiguracao("x", "y")));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddIdentityInfraestrutura(null!));
    }

    /// <summary>
    /// Monta uma configuração em memória com as chaves informadas.
    /// </summary>
    /// <param name="conexao">Valor de <c>ConnectionStrings:Identity</c>.</param>
    /// <param name="chave">Valor de <c>Autenticacao:ChaveHashEmail</c>.</param>
    /// <param name="emissor">Valor de <c>Jwt:Issuer</c>.</param>
    /// <param name="audiencia">Valor de <c>Jwt:Audience</c>.</param>
    /// <returns>A configuração montada.</returns>
    private static IConfiguration CriarConfiguracao(string? conexao, string? chave, string? emissor = null, string? audiencia = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = conexao,
                ["Autenticacao:ChaveHashEmail"] = chave,
                ["Cpf:ChaveCifra"] = _chaveCpf,
                ["Cpf:ChaveHash"] = _chaveCpf,
                ["Cpf:VersaoChave"] = "2",
                ["Jwt:Issuer"] = emissor,
                ["Jwt:Audience"] = audiencia,
            })
            .Build();
    }
}
