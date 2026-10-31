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
    /// Faz login com as credenciais do último doador cadastrado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele faz login com essas credenciais")]
    public async Task QuandoEleFazLoginComEssasCredenciais()
    {
        await ambiente.LoginAsync(ambiente.UltimoEmail!, ambiente.UltimaSenha!);
    }

    /// <summary>
    /// Faz login com outro e-mail (por exemplo, em outra caixa) e a senha correta.
    /// </summary>
    /// <param name="email">E-mail digitado.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele faz login com o e-mail {string} e a senha correta")]
    public async Task QuandoEleFazLoginComEmailESenhaCorreta(string email)
    {
        await ambiente.LoginAsync(email, ambiente.UltimaSenha!);
    }

    /// <summary>
    /// Faz login com o e-mail do último doador e a senha informada.
    /// </summary>
    /// <param name="senha">Senha digitada.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele faz login com a senha {string}")]
    public async Task QuandoEleFazLoginComASenha(string senha)
    {
        await ambiente.LoginAsync(ambiente.UltimoEmail!, senha);
    }

    /// <summary>
    /// Faz o número de tentativas informado com senha incorreta para o e-mail. Como só existe senha incorreta para quem
    /// tem cadastro, o doador é cadastrado antes com a senha padrão, a mesma usada depois como "senha correta".
    /// </summary>
    /// <param name="quantidade">Quantidade de tentativas.</param>
    /// <param name="email">E-mail que sofre as tentativas.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("{int} tentativas de login com senha incorreta para {string} em 15 minutos")]
    public async Task DadoTentativasComSenhaIncorreta(int quantidade, string email)
    {
        if (ambiente.UltimoEmail != email)
        {
            await ambiente.CadastrarAsync("Maria Silva", email, "529.982.247-25", _senhaPadrao, aceite: true);
            ambiente.UltimoEmail = email;
            ambiente.UltimaSenha = _senhaPadrao;
        }

        for (var i = 0; i < quantidade; i++)
        {
            await ambiente.LoginAsync(email, "senha-errada");
        }
    }

    /// <summary>
    /// Tenta entrar com a senha correta do último doador.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele tenta entrar novamente com a senha correta")]
    public async Task QuandoEleTentaEntrarNovamenteComASenhaCorreta()
    {
        await ambiente.LoginAsync(ambiente.UltimoEmail!, ambiente.UltimaSenha!);
    }

    /// <summary>
    /// Confere a claim de papel do token devolvido.
    /// </summary>
    /// <param name="role">Papel esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o token contém a role {string}")]
    public async Task EntaoOTokenContemARole(string role)
    {
        (await ambiente.LerPayloadDoTokenAsync()).ShouldContain($"\"role\":\"{role}\"");
    }

    /// <summary>
    /// Confere que o token não carrega nome, e-mail nem CPF.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("o token não contém nome, e-mail nem CPF")]
    public async Task EntaoOTokenNaoContemDadosPessoais()
    {
        var payload = await ambiente.LerPayloadDoTokenAsync();

        payload.ShouldNotContain("Maria");
        payload.ShouldNotContain("exemplo.com");
        payload.ShouldNotContain("52998224725");
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
