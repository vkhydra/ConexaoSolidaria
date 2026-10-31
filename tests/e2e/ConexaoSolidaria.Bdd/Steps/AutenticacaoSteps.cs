using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 001 (login e controle de acesso), declarativos e sem detalhes de HTTP no texto.
/// </summary>
/// <param name="ambiente">Ambiente do cenário, injetado pelo Reqnroll.</param>
[Binding]
public sealed class AutenticacaoSteps(AmbienteIdentity ambiente)
{
    /// <summary>
    /// Senha usada quando o cenário não informa uma.
    /// </summary>
    private const string _senhaPadrao = "Senha@123";

    /// <summary>
    /// Registra o e-mail e a senha do gestor inicial, sem iniciar a API.
    /// </summary>
    [Given("as variáveis de seed do gestor configuradas")]
    public void DadoAsVariaveisDeSeedConfiguradas()
    {
        ambiente.SeedEmail = "gestor.inicial@exemplo.com";
        ambiente.SeedSenha = "SenhaInicial@2026";
    }

    /// <summary>
    /// Inicia a API com o banco vazio; o seed roda na inicialização.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("a Identity.Api inicia sem nenhum gestor no banco")]
    public async Task QuandoAApiIniciaSemGestor()
    {
        using var cliente = await ambiente.IniciarApiAsync();
    }

    /// <summary>
    /// Faz login com o e-mail informado e uma senha qualquer.
    /// </summary>
    /// <param name="email">E-mail digitado.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("alguém faz login com o e-mail {string}")]
    public async Task QuandoAlguemFazLoginComOEmail(string email)
    {
        await ambiente.LoginAsync(email, _senhaPadrao);
    }

    /// <summary>
    /// Confere o status HTTP da última resposta.
    /// </summary>
    /// <param name="status">Status esperado.</param>
    [Then("recebe status {int}")]
    public void EntaoRecebeStatus(int status)
    {
        ambiente.Resposta.ShouldNotBeNull();
        ambiente.Resposta.StatusCode.ShouldBe((HttpStatusCode)status);
    }

    /// <summary>
    /// Confere a mensagem (título) da resposta de erro.
    /// </summary>
    /// <param name="mensagem">Mensagem esperada.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("a mensagem é {string}")]
    public async Task EntaoAMensagemE(string mensagem)
    {
        (await ambiente.LerTituloAsync()).ShouldBe(mensagem);
    }

    /// <summary>
    /// Confere que existe exatamente um gestor, com o papel esperado.
    /// </summary>
    /// <param name="role">Papel esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o gestor é criado com a role {string}")]
    public async Task EntaoOGestorECriadoComARole(string role)
    {
        (await ambiente.ListarPapeisDeGestorAsync()).ShouldBe([role]);
    }

    /// <summary>
    /// Inicia uma segunda instância da API e confere que o seed não criou outro gestor.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("uma segunda inicialização não cria outro gestor")]
    public async Task EntaoUmaSegundaInicializacaoNaoCriaOutroGestor()
    {
        using var cliente = await ambiente.IniciarApiAsync();

        (await ambiente.ListarPapeisDeGestorAsync()).Count.ShouldBe(1);
    }
}
