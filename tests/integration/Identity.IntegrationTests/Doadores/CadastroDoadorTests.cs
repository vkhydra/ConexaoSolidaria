using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.IntegrationTests.Suporte;

namespace Identity.IntegrationTests.Doadores;

/// <summary>
/// Testes de ponta a ponta do <c>POST /api/v1/doadores</c>: API real, PostgreSQL em container e saída de log conferida.
/// </summary>
/// <param name="api">API compartilhada pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class CadastroDoadorTests(IdentityApiFixture api) : IClassFixture<IdentityApiFixture>
{
    /// <summary>
    /// Rota do cadastro.
    /// </summary>
    private const string _rota = "/api/v1/doadores";

    /// <summary>
    /// Senha válida usada nos cenários.
    /// </summary>
    private const string _senha = "Senha@123";

    /// <summary>
    /// Dados válidos, quando o visitante se cadastra, então recebe 201 com o CPF mascarado, e-mail normalizado, Location e sem senha.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Cadastro_DadosValidos_Devolve201()
    {
        using var cliente = api.CreateClient();
        var email = $"Maria{Guid.NewGuid():N}@Exemplo.com";

        var resposta = await cliente.PostAsJsonAsync(_rota, Corpo(email: email, cpf: "529.982.247-25"), TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        resposta.Headers.Location!.OriginalString.ShouldBe("/api/v1/doadores/me");
        var texto = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var corpo = JsonSerializer.Deserialize<JsonElement>(texto);
        corpo.GetProperty("email").GetString().ShouldBe(email.ToLowerInvariant());
        corpo.GetProperty("cpfMascarado").GetString().ShouldBe("***.982.247-**");
        corpo.GetProperty("nomeCompleto").GetString().ShouldBe("Maria Silva");
        texto.ShouldNotContain(_senha);
        texto.ShouldNotContain("52998224725");
    }

    /// <summary>
    /// Dado um doador cadastrado, quando o banco é inspecionado, então o CPF está cifrado e nem a senha nem o CPF aparecem nos logs.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Cadastro_Sucesso_CpfCifradoNoBancoELogsSemDadosPessoais()
    {
        using var cliente = api.CreateClient();
        var email = $"log{Guid.NewGuid():N}@exemplo.com";
        var resposta = await cliente.PostAsJsonAsync(_rota, Corpo(email: email, cpf: "111.444.777-35", nome: "Joao Sobrenome Unico"), TestContext.Current.CancellationToken);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var cifrado = await api.LerCpfCifradoAsync(email);

        cifrado.ShouldNotBeNull();
        System.Text.Encoding.UTF8.GetString(cifrado).ShouldNotContain("11144477735");
        api.TextosDeLog.ShouldNotBeEmpty();
        api.TextosDeLog.ShouldAllBe(t => !t.Contains(email, StringComparison.OrdinalIgnoreCase)
            && !t.Contains("11144477735", StringComparison.Ordinal)
            && !t.Contains("111.444.777-35", StringComparison.Ordinal)
            && !t.Contains(_senha, StringComparison.Ordinal)
            && !t.Contains("Sobrenome Unico", StringComparison.Ordinal));
    }

    /// <summary>
    /// Dado um e-mail cadastrado, quando outro visitante usa o mesmo e-mail em outra caixa, então recebe 409 doador.email_em_uso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Cadastro_EmailDuplicado_Devolve409()
    {
        using var cliente = api.CreateClient();
        var sufixo = Guid.NewGuid().ToString("N");
        (await cliente.PostAsJsonAsync(_rota, Corpo(email: $"dup{sufixo}@exemplo.com", cpf: "123.456.789-09"), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var resposta = await cliente.PostAsJsonAsync(_rota, Corpo(email: $"DUP{sufixo}@exemplo.com", cpf: "390.533.447-05"), TestContext.Current.CancellationToken);

        await ConferirConflitoAsync(resposta, "doador.email_em_uso");
    }

    /// <summary>
    /// Dado um CPF cadastrado, quando outro visitante o usa sem máscara, então recebe 409 doador.cpf_em_uso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Cadastro_CpfDuplicadoComOutraMascara_Devolve409()
    {
        using var cliente = api.CreateClient();
        (await cliente.PostAsJsonAsync(_rota, Corpo(email: $"a{Guid.NewGuid():N}@exemplo.com", cpf: "935.411.347-80"), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var resposta = await cliente.PostAsJsonAsync(_rota, Corpo(email: $"b{Guid.NewGuid():N}@exemplo.com", cpf: "93541134780"), TestContext.Current.CancellationToken);

        await ConferirConflitoAsync(resposta, "doador.cpf_em_uso");
    }

    /// <summary>
    /// Dados inválidos (CPF, senha, e-mail, nome ou falta de aceite), quando enviados, então recebe 400 com o campo certo e sem repetir o valor.
    /// </summary>
    /// <param name="campo">Campo esperado em <c>errors</c>.</param>
    /// <param name="nome">Nome enviado.</param>
    /// <param name="email">E-mail enviado.</param>
    /// <param name="cpf">CPF enviado.</param>
    /// <param name="senha">Senha enviada.</param>
    /// <param name="aceite">Aceite enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("cpf", "Maria Silva", "m@exemplo.com", "529.982.247-26", _senha, true)]
    [InlineData("cpf", "Maria Silva", "m@exemplo.com", "111.111.111-11", _senha, true)]
    [InlineData("cpf", "Maria Silva", "m@exemplo.com", "123", _senha, true)]
    [InlineData("senha", "Maria Silva", "m@exemplo.com", "529.982.247-25", "curta@1", true)]
    [InlineData("senha", "Maria Silva", "m@exemplo.com", "529.982.247-25", "semnumero@", true)]
    [InlineData("senha", "Maria Silva", "m@exemplo.com", "529.982.247-25", "Sem1Especial", true)]
    [InlineData("email", "Maria Silva", " m@exemplo.com", "529.982.247-25", _senha, true)]
    [InlineData("nomeCompleto", "Maria", "m@exemplo.com", "529.982.247-25", _senha, true)]
    [InlineData("aceitePoliticaPrivacidade", "Maria Silva", "m@exemplo.com", "529.982.247-25", _senha, false)]
    public async Task Cadastro_DadosInvalidos_Devolve400(string campo, string nome, string email, string cpf, string senha, bool aceite)
    {
        using var cliente = api.CreateClient();
        var corpo = new { nomeCompleto = nome, email, cpf, senha, aceitePoliticaPrivacidade = aceite };

        var resposta = await cliente.PostAsJsonAsync(_rota, corpo, TestContext.Current.CancellationToken);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var texto = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var json = JsonSerializer.Deserialize<JsonElement>(texto);
        json.GetProperty("code").GetString().ShouldBe("validacao");
        json.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe([campo]);
        texto.ShouldNotContain(cpf);
        texto.ShouldNotContain(senha);
    }

    /// <summary>
    /// Monta um corpo de cadastro válido, trocando só o que o teste precisa.
    /// </summary>
    /// <param name="email">E-mail do cadastro.</param>
    /// <param name="cpf">CPF do cadastro.</param>
    /// <param name="nome">Nome do cadastro.</param>
    /// <returns>O corpo da requisição.</returns>
    private static object Corpo(string email, string cpf, string nome = "Maria Silva")
    {
        return new { nomeCompleto = nome, email, cpf, senha = _senha, aceitePoliticaPrivacidade = true };
    }

    /// <summary>
    /// Confere uma resposta 409 com o código estável esperado.
    /// </summary>
    /// <param name="resposta">Resposta HTTP.</param>
    /// <param name="codigo">Código esperado.</param>
    /// <returns>Tarefa da conferência.</returns>
    private static async Task ConferirConflitoAsync(HttpResponseMessage resposta, string codigo)
    {
        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        corpo.GetProperty("code").GetString().ShouldBe(codigo);
    }
}
