using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.IntegrationTests.Suporte;

namespace Identity.IntegrationTests.Titular;

/// <summary>
/// Testes de ponta a ponta dos direitos do titular: acesso aos dados e anonimização, com a API real e o PostgreSQL em container.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class TitularTests(IdentityApiFixture api) : IClassFixture<IdentityApiFixture>
{
    /// <summary>
    /// Rota dos dados do titular.
    /// </summary>
    private const string _rotaMe = "/api/v1/doadores/me";

    /// <summary>
    /// Senha dos doadores de teste.
    /// </summary>
    private const string _senha = "Senha@123";

    /// <summary>
    /// Dado um doador autenticado, quando consulta os próprios dados, então vê o CPF mascarado, o aceite e não recebe cache.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Consultar_DoadorAutenticado_Devolve200ComCpfMascarado()
    {
        var doador = await CadastrarEEntrarAsync("529.982.247-25");

        var resposta = await doador.Cliente.GetAsync(_rotaMe, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        resposta.Headers.CacheControl!.NoStore.ShouldBeTrue();
        var texto = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var corpo = JsonSerializer.Deserialize<JsonElement>(texto);
        corpo.GetProperty("email").GetString().ShouldBe(doador.Email);
        corpo.GetProperty("cpfMascarado").GetString().ShouldBe("***.982.247-**");
        corpo.GetProperty("consentimento").GetProperty("versao").GetString().ShouldBe("2026-10");
        corpo.GetProperty("consentimento").GetProperty("aceitoEm").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
        texto.ShouldNotContain("52998224725");
    }

    /// <summary>
    /// Dado um doador autenticado, quando solicita a exclusão, então recebe 204 e as colunas pessoais ficam anonimizadas no banco.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Excluir_DoadorAutenticado_Devolve204EAnonimizaColunas()
    {
        var doador = await CadastrarEEntrarAsync("111.444.777-35");

        var resposta = await doador.Cliente.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        resposta.Headers.CacheControl!.NoStore.ShouldBeTrue();
        var colunas = await api.LerColunasPessoaisAsync(doador.Id);
        colunas.NomeCompleto.ShouldBe("Doador anonimizado");
        colunas.Email.ShouldBe($"anonimo+{doador.Id}@invalid");
        colunas.CpfCifrado.ShouldBeNull();
        colunas.CpfHash.ShouldBeNull();
        colunas.SenhaHash.ShouldBeNull();
        colunas.AnonimizadoEm.ShouldNotBeNull();
    }

    /// <summary>
    /// Dado um doador que anonimizou o cadastro, quando tenta entrar ou usa o token antigo, então recebe 401 em ambos e em nova exclusão.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task AposAnonimizar_LoginETokenAntigoRecebem401()
    {
        var doador = await CadastrarEEntrarAsync("123.456.789-09");
        (await doador.Cliente.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = doador.Email, senha = _senha }, TestContext.Current.CancellationToken);
        var consulta = await doador.Cliente.GetAsync(_rotaMe, TestContext.Current.CancellationToken);
        var outraExclusao = await doador.Cliente.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken);

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        consulta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        outraExclusao.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problema = await consulta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        problema.GetProperty("code").GetString().ShouldBe("auth.nao_autenticado");
    }

    /// <summary>
    /// Dado um doador anonimizado, quando um visitante se cadastra com o mesmo e-mail e CPF, então recebe 201.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task AposAnonimizar_EmailECpfPodemSerReutilizados()
    {
        var doador = await CadastrarEEntrarAsync("390.533.447-05");
        (await doador.Cliente.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var novo = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/doadores",
            new { nomeCompleto = "Maria Nova", email = doador.Email, cpf = "390.533.447-05", senha = _senha, aceitePoliticaPrivacidade = true },
            TestContext.Current.CancellationToken);

        novo.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>
    /// Dado um gestor autenticado ou ninguém autenticado, quando acessa as rotas do titular, então recebe 403 e 401.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Rotas_GestorOuAnonimo_Recebem403Ou401()
    {
        var email = await api.CriarGestorAsync(_senha);
        using var gestor = api.CreateClient();
        var login = await gestor.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = _senha }, TestContext.Current.CancellationToken);
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        gestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString());
        using var anonimo = api.CreateClient();

        (await gestor.GetAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await gestor.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await anonimo.GetAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonimo.DeleteAsync(_rotaMe, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Cadastra um doador com o CPF informado e entra com ele.
    /// </summary>
    /// <param name="cpf">CPF do doador.</param>
    /// <returns>O identificador, o e-mail e um cliente já autenticado.</returns>
    private async Task<(Guid Id, string Email, HttpClient Cliente)> CadastrarEEntrarAsync(string cpf)
    {
        var email = $"titular-{Guid.NewGuid():N}@exemplo.com";
        var cliente = api.CreateClient();
        var cadastro = await cliente.PostAsJsonAsync(
            "/api/v1/doadores",
            new { nomeCompleto = "Maria Silva", email, cpf, senha = _senha, aceitePoliticaPrivacidade = true },
            TestContext.Current.CancellationToken);
        cadastro.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await cadastro.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();

        var login = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = _senha }, TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("accessToken").GetString();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (id, email, cliente);
    }
}
