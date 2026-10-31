using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 003 (cadastro de doador) e dos passos compartilhados de doador cadastrado.
/// </summary>
/// <param name="ambiente">Ambiente do cenário, injetado pelo Reqnroll.</param>
[Binding]
public sealed class CadastroDoadorSteps(AmbienteIdentity ambiente)
{
    /// <summary>
    /// Nome do doador pré-cadastrado e do visitante dos cenários.
    /// </summary>
    private const string _nomePadrao = "Maria Silva";

    /// <summary>
    /// Senha padrão dos cenários.
    /// </summary>
    private const string _senhaPadrao = "Senha@123";

    /// <summary>
    /// CPF do doador pré-cadastrado.
    /// </summary>
    private const string _cpfDoDoador = "529.982.247-25";

    /// <summary>
    /// E-mail do doador pré-cadastrado.
    /// </summary>
    private const string _emailDoDoador = "maria@exemplo.com";

    /// <summary>
    /// CPF do visitante que se cadastra, diferente do doador pré-cadastrado.
    /// </summary>
    private const string _cpfDoVisitante = "111.444.777-35";

    /// <summary>
    /// E-mail do visitante que se cadastra, diferente do doador pré-cadastrado.
    /// </summary>
    private const string _emailDoVisitante = "joao@exemplo.com";

    /// <summary>
    /// Cadastra um doador com o e-mail e a senha informados, e guarda as credenciais para o login.
    /// </summary>
    /// <param name="email">E-mail do doador.</param>
    /// <param name="senha">Senha do doador.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador cadastrado com e-mail {string} e senha {string}")]
    public async Task DadoUmDoadorCadastradoComEmailESenha(string email, string senha)
    {
        await CadastrarDoadorAsync(email, _cpfDoDoador, senha);
    }

    /// <summary>
    /// Cadastra um doador com o e-mail informado e a senha padrão.
    /// </summary>
    /// <param name="email">E-mail do doador.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador cadastrado com e-mail {string}")]
    public async Task DadoUmDoadorCadastradoComEmail(string email)
    {
        await CadastrarDoadorAsync(email, _cpfDoDoador, _senhaPadrao);
    }

    /// <summary>
    /// Cadastra um doador com o CPF informado.
    /// </summary>
    /// <param name="cpf">CPF do doador.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador cadastrado com CPF {string}")]
    public async Task DadoUmDoadorCadastradoComCpf(string cpf)
    {
        await CadastrarDoadorAsync(_emailDoDoador, cpf, _senhaPadrao);
    }

    /// <summary>
    /// Cadastra um visitante com todos os campos informados.
    /// </summary>
    /// <param name="nome">Nome completo.</param>
    /// <param name="email">E-mail.</param>
    /// <param name="cpf">CPF.</param>
    /// <param name="senha">Senha.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante se cadastra com nome {string}, e-mail {string}, CPF {string} e senha {string}")]
    public async Task QuandoUmVisitanteSeCadastraComTudo(string nome, string email, string cpf, string senha)
    {
        await ambiente.CadastrarAsync(nome, email, cpf, senha, aceite: true);
    }

    /// <summary>
    /// Cadastra outro visitante com o e-mail informado.
    /// </summary>
    /// <param name="email">E-mail.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("outro visitante se cadastra com o e-mail {string}")]
    public async Task QuandoOutroVisitanteSeCadastraComEmail(string email)
    {
        await ambiente.CadastrarAsync(_nomePadrao, email, _cpfDoVisitante, _senhaPadrao, aceite: true);
    }

    /// <summary>
    /// Cadastra outro visitante com o CPF informado.
    /// </summary>
    /// <param name="cpf">CPF.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("outro visitante se cadastra com o CPF {string}")]
    public async Task QuandoOutroVisitanteSeCadastraComCpf(string cpf)
    {
        await ambiente.CadastrarAsync(_nomePadrao, _emailDoVisitante, cpf, _senhaPadrao, aceite: true);
    }

    /// <summary>
    /// Cadastra um visitante com o CPF informado e o restante válido.
    /// </summary>
    /// <param name="cpf">CPF.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante se cadastra com CPF {string}")]
    public async Task QuandoUmVisitanteSeCadastraComCpf(string cpf)
    {
        await ambiente.CadastrarAsync(_nomePadrao, _emailDoVisitante, cpf, _senhaPadrao, aceite: true);
    }

    /// <summary>
    /// Cadastra um visitante com a senha informada e o restante válido.
    /// </summary>
    /// <param name="senha">Senha.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante se cadastra com a senha {string}")]
    public async Task QuandoUmVisitanteSeCadastraComSenha(string senha)
    {
        await ambiente.CadastrarAsync(_nomePadrao, _emailDoVisitante, _cpfDoVisitante, senha, aceite: true);
    }

    /// <summary>
    /// Cadastra um visitante que não marcou o aceite da política de privacidade.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante se cadastra sem aceitar a política de privacidade")]
    public async Task QuandoUmVisitanteSeCadastraSemAceite()
    {
        await ambiente.CadastrarAsync(_nomePadrao, _emailDoVisitante, _cpfDoVisitante, _senhaPadrao, aceite: false);
    }

    /// <summary>
    /// Confere o e-mail gravado no banco.
    /// </summary>
    /// <param name="email">E-mail esperado, já normalizado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o e-mail é salvo como {string}")]
    public async Task EntaoOEmailESalvoComo(string email)
    {
        (await ambiente.LerUsuarioGravadoAsync(email)).Email.ShouldBe(email);
    }

    /// <summary>
    /// Confere que a senha gravada é um hash BCrypt e não o texto digitado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("a senha não é salva em texto puro")]
    public async Task EntaoASenhaNaoESalvaEmTextoPuro()
    {
        var gravado = await ambiente.LerUsuarioGravadoAsync("maria@exemplo.com");

        gravado.SenhaHash.ShouldNotBeNull();
        gravado.SenhaHash.ShouldNotBe(_senhaPadrao);
        gravado.SenhaHash.ShouldStartWith("$2");
    }

    /// <summary>
    /// Confere o CPF mascarado devolvido na resposta.
    /// </summary>
    /// <param name="cpfMascarado">CPF mascarado esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("a resposta mostra o CPF como {string}")]
    public async Task EntaoARespostaMostraOCpfComo(string cpfMascarado)
    {
        (await ambiente.LerCampoAsync("cpfMascarado")).ShouldBe(cpfMascarado);
    }

    /// <summary>
    /// Confere o código estável do erro devolvido.
    /// </summary>
    /// <param name="codigo">Código esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o código do erro é {string}")]
    public async Task EntaoOCodigoDoErroE(string codigo)
    {
        (await ambiente.LerCampoAsync("code")).ShouldBe(codigo);
    }

    /// <summary>
    /// Cadastra um doador e exige que o cadastro tenha dado certo.
    /// </summary>
    /// <param name="email">E-mail do doador.</param>
    /// <param name="cpf">CPF do doador.</param>
    /// <param name="senha">Senha do doador.</param>
    /// <returns>Tarefa do passo.</returns>
    private async Task CadastrarDoadorAsync(string email, string cpf, string senha)
    {
        await ambiente.CadastrarAsync(_nomePadrao, email, cpf, senha, aceite: true);
        ambiente.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Created);
        ambiente.UltimoEmail = email;
        ambiente.UltimaSenha = senha;
    }
}
