using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 008 (direitos do titular): ver os próprios dados e anonimizar o cadastro.
/// </summary>
/// <param name="ambiente">Ambiente do cenário, injetado pelo Reqnroll.</param>
[Binding]
public sealed class DireitosDoTitularSteps(AmbienteIdentity ambiente)
{
    /// <summary>
    /// Senha padrão dos doadores dos cenários.
    /// </summary>
    private const string _senhaPadrao = "Senha@123";

    /// <summary>
    /// E-mail do doador autenticado dos cenários.
    /// </summary>
    private const string _emailPadrao = "titular@exemplo.com";

    /// <summary>
    /// Cadastra um doador com o CPF informado e entra com ele.
    /// </summary>
    /// <param name="cpf">CPF do doador.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador autenticado cadastrado com o CPF {string}")]
    public async Task DadoUmDoadorAutenticadoComCpf(string cpf)
    {
        await CadastrarEEntrarAsync(_emailPadrao, cpf);
    }

    /// <summary>
    /// Cadastra um doador, entra e anonimiza o cadastro; o token fica guardado para ser reutilizado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador que acabou de anonimizar o cadastro")]
    public async Task DadoUmDoadorQueAcabouDeAnonimizar()
    {
        await CadastrarEEntrarAsync(_emailPadrao, "529.982.247-25");
        await ambiente.ExcluirMeusDadosAsync();
        ambiente.Resposta!.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Cadastra um doador com o e-mail informado, entra e anonimiza o cadastro.
    /// </summary>
    /// <param name="email">E-mail que o doador usava.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador anonimizado que usava o e-mail {string}")]
    public async Task DadoUmDoadorAnonimizadoQueUsavaOEmail(string email)
    {
        await CadastrarEEntrarAsync(email, "529.982.247-25");
        await ambiente.ExcluirMeusDadosAsync();
        ambiente.Resposta!.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Consulta os próprios dados com o token em uso.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele consulta os seus dados")]
    public async Task QuandoEleConsultaOsSeusDados()
    {
        await ambiente.ConsultarMeusDadosAsync();
    }

    /// <summary>
    /// Consulta os próprios dados com o mesmo token de antes da anonimização.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele consulta os seus dados com o mesmo token")]
    public async Task QuandoEleConsultaOsSeusDadosComOMesmoToken()
    {
        await ambiente.ConsultarMeusDadosAsync();
    }

    /// <summary>
    /// Solicita a exclusão dos próprios dados.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele solicita a exclusão dos seus dados")]
    public async Task QuandoEleSolicitaAExclusaoDosSeusDados()
    {
        await ambiente.ExcluirMeusDadosAsync();
    }

    /// <summary>
    /// Cadastra um visitante com o e-mail informado e dados novos de CPF.
    /// </summary>
    /// <param name="email">E-mail do visitante.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante se cadastra com o e-mail {string}")]
    public async Task QuandoUmVisitanteSeCadastraComOEmail(string email)
    {
        await ambiente.CadastrarAsync("Maria Nova", email, "529.982.247-25", _senhaPadrao, aceite: true);
    }

    /// <summary>
    /// Confere o CPF mascarado devolvido na consulta.
    /// </summary>
    /// <param name="cpfMascarado">CPF mascarado esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("vê o CPF como {string}")]
    public async Task EntaoVeOCpfComo(string cpfMascarado)
    {
        (await ambiente.LerCampoAsync("cpfMascarado")).ShouldBe(cpfMascarado);
    }

    /// <summary>
    /// Confere que a versão e a data do aceite aparecem na consulta.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("vê a versão e a data do aceite da política de privacidade")]
    public async Task EntaoVeAVersaoEADataDoAceite()
    {
        (await ambiente.LerCampoAninhadoAsync("consentimento", "versao")).ShouldBe("2026-10");
        DateTimeOffset.TryParse(await ambiente.LerCampoAninhadoAsync("consentimento", "aceitoEm"), out _).ShouldBeTrue();
    }

    /// <summary>
    /// Confere que o login do doador anonimizado é recusado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("não consegue mais fazer login")]
    public async Task EntaoNaoConsegueMaisFazerLogin()
    {
        await ambiente.LoginAsync(ambiente.UltimoEmail!, ambiente.UltimaSenha!);

        ambiente.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Cadastra um doador, entra com ele e guarda o token para as chamadas seguintes.
    /// </summary>
    /// <param name="email">E-mail do doador.</param>
    /// <param name="cpf">CPF do doador.</param>
    /// <returns>Tarefa auxiliar.</returns>
    private async Task CadastrarEEntrarAsync(string email, string cpf)
    {
        await ambiente.CadastrarAsync("Maria Silva", email, cpf, _senhaPadrao, aceite: true);
        ambiente.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Created);
        ambiente.UltimoEmail = email;
        ambiente.UltimaSenha = _senhaPadrao;
        await ambiente.AutenticarAsync(email, _senhaPadrao);
    }
}
