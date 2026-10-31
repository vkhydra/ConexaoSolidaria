using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 002 (gestão de campanhas) e dos cenários de acesso por papel da feature 001.
/// </summary>
/// <param name="identidade">Ambiente da Identity, que emite os tokens.</param>
/// <param name="campanhas">Ambiente da Campanhas.Api.</param>
[Binding]
public sealed class GestaoDeCampanhasSteps(AmbienteIdentity identidade, AmbienteCampanhas campanhas)
{
    /// <summary>
    /// Data de início usada quando o cenário só informa a data fim; anterior a qualquer data fim dos exemplos.
    /// </summary>
    private const string _inicioPadrao = "2026-11-01";

    /// <summary>
    /// Autentica um gestor (criado pelo seed da Identity) e passa a usar o token dele.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Given("um gestor autenticado")]
    public async Task DadoUmGestorAutenticado()
    {
        await campanhas.UsarTokenAsync(await identidade.ObterTokenDoGestorAsync());
    }

    /// <summary>
    /// Autentica um doador (cadastrado pela Identity) e passa a usar o token dele.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Given("um doador autenticado")]
    public async Task DadoUmDoadorAutenticado()
    {
        await campanhas.UsarTokenAsync(await identidade.ObterTokenDeDoadorAsync());
    }

    /// <summary>
    /// Fixa a data de hoje para a API de Campanhas.
    /// </summary>
    /// <param name="hoje">Data de hoje (<c>aaaa-MM-dd</c>).</param>
    [Given("hoje é {string}")]
    public void DadoHojeE(string hoje)
    {
        campanhas.DefinirHoje(hoje);
    }

    /// <summary>
    /// Cria a campanha como pré-condição do cenário e exige sucesso.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="versao">Rótulo da versão (apenas "1" é válido: a primeira).</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha {string} ativa na versão {string}")]
    public async Task DadoACampanhaAtivaNaVersao(string titulo, string versao)
    {
        versao.ShouldBe("1");
        await CriarComoPreCondicaoAsync(titulo);
    }

    /// <summary>
    /// Cria a campanha e a altera, para que a versão "1" fique desatualizada.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="versao">Rótulo da versão atual (apenas "2" é válido: a segunda).</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha {string} já alterada para a versão {string}")]
    public async Task DadoACampanhaJaAlteradaParaAVersao(string titulo, string versao)
    {
        versao.ShouldBe("2");
        await CriarComoPreCondicaoAsync(titulo);
        await campanhas.AlterarMetaAsync("11000.00", "1");
        ((int)UltimaResposta().StatusCode).ShouldBe(204);
    }

    /// <summary>
    /// Cria uma campanha e a conclui.
    /// </summary>
    /// <param name="status">Status desejado (apenas "Concluida" é usado).</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("uma campanha com status {string}")]
    public async Task DadoUmaCampanhaComStatus(string status)
    {
        status.ShouldBe("Concluida");
        await CriarComoPreCondicaoAsync("Campanha Concluída");
        await campanhas.AlterarStatusAsync("Concluida");
        UltimaResposta().StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Cria uma campanha com título, período e meta.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="inicio">Data início.</param>
    /// <param name="fim">Data fim.</param>
    /// <param name="meta">Meta em reais.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele cria a campanha \"(.*)\" de \"(.*)\" a \"(.*)\" com meta (.*)")]
    public async Task QuandoEleCriaACampanha(string titulo, string inicio, string fim, string meta)
    {
        await campanhas.CriarAsync(titulo, inicio, fim, meta);
    }

    /// <summary>
    /// Cria uma campanha em que só a data fim e a meta importam.
    /// </summary>
    /// <param name="fim">Data fim.</param>
    /// <param name="meta">Meta em reais.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele cria uma campanha com data fim \"(.*)\" e meta (.*)")]
    public async Task QuandoEleCriaUmaCampanhaComDataFimEMeta(string fim, string meta)
    {
        await campanhas.CriarAsync("Campanha de teste", _inicioPadrao, fim, meta);
    }

    /// <summary>
    /// Cria uma campanha com período explícito e meta.
    /// </summary>
    /// <param name="inicio">Data início.</param>
    /// <param name="fim">Data fim.</param>
    /// <param name="meta">Meta em reais.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele cria uma campanha de \"(.*)\" a \"(.*)\" com meta (.*)")]
    public async Task QuandoEleCriaUmaCampanhaDePeriodo(string inicio, string fim, string meta)
    {
        await campanhas.CriarAsync("Campanha de teste", inicio, fim, meta);
    }

    /// <summary>
    /// Tenta criar uma campanha válida com o token em uso.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele tenta criar uma campanha")]
    public async Task QuandoEleTentaCriarUmaCampanha()
    {
        await campanhas.CriarAsync("Campanha de teste", "2026-11-10", "2026-12-24", "1000.00");
    }

    /// <summary>
    /// Altera a meta informando a versão que o gestor viu.
    /// </summary>
    /// <param name="meta">Nova meta.</param>
    /// <param name="versao">Rótulo da versão enviada.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele altera a meta para (.*) informando a versão \"(.*)\"")]
    public async Task QuandoEleAlteraAMeta(string meta, string versao)
    {
        await campanhas.AlterarMetaAsync(meta, versao);
    }

    /// <summary>
    /// Altera a campanha informando uma versão antiga.
    /// </summary>
    /// <param name="versao">Rótulo da versão enviada.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele altera a campanha informando a versão {string}")]
    public async Task QuandoEleAlteraACampanhaInformandoAVersao(string versao)
    {
        await campanhas.AlterarMetaAsync("12000.00", versao);
    }

    /// <summary>
    /// Tenta cancelar a última campanha.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele tenta cancelar a campanha")]
    public async Task QuandoEleTentaCancelarACampanha()
    {
        await campanhas.AlterarStatusAsync("Cancelada");
    }

    /// <summary>
    /// Lista as campanhas da gestão sem enviar token.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("alguém lista as campanhas da gestão sem token")]
    public async Task QuandoAlguemListaAsCampanhasSemToken()
    {
        await campanhas.UsarTokenAsync(null);
        await campanhas.ListarAsync();
    }

    /// <summary>
    /// Lista as campanhas da gestão com o token em uso.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("ele lista as campanhas da gestão")]
    public async Task QuandoEleListaAsCampanhasDaGestao()
    {
        await campanhas.ListarAsync();
    }

    /// <summary>
    /// Confere o status e o total arrecadado da campanha criada.
    /// </summary>
    /// <param name="status">Status esperado.</param>
    /// <param name="total">Total arrecadado esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("a campanha tem status \"(.*)\" e total arrecadado (.*)")]
    public async Task EntaoACampanhaTemStatusETotal(string status, string total)
    {
        (await campanhas.LerCampoAsync("status")).ShouldBe(status);
        (await campanhas.LerCampoAsync("totalArrecadado")).ShouldBe(total);
    }

    /// <summary>
    /// Cria uma campanha válida como pré-condição e exige 201.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa auxiliar.</returns>
    private async Task CriarComoPreCondicaoAsync(string titulo)
    {
        await campanhas.CriarAsync(titulo, "2026-11-10", "2026-12-24", "10000.00");
        UltimaResposta().StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>
    /// Obtém a última resposta HTTP do cenário.
    /// </summary>
    /// <returns>A última resposta.</returns>
    private HttpResponseMessage UltimaResposta()
    {
        return identidade.Resposta!;
    }
}
