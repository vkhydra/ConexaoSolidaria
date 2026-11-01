using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 006 (busca de campanhas), vividos por um visitante sem autenticação. As campanhas são
/// criadas pela API por um gestor; o Outbox é entregue ao índice por uma ponte que faz o papel do RabbitMQ e do consumidor.
/// </summary>
/// <param name="identidade">Ambiente da Identity, que emite o token do gestor.</param>
/// <param name="campanhas">Ambiente da Campanhas.Api.</param>
/// <param name="resposta">Última resposta do cenário.</param>
[Binding]
public sealed class BuscaDeCampanhasSteps(AmbienteIdentity identidade, AmbienteCampanhas campanhas, RespostaDoCenario resposta)
{
    /// <summary>
    /// Faz os cenários marcados com <c>@busca</c> usarem o Elasticsearch real do container.
    /// </summary>
    [BeforeScenario("busca", Order = 0)]
    public void UsarElasticsearchReal()
    {
        campanhas.UsarElasticsearch = true;
        campanhas.DefinirHoje("2026-11-10");
    }

    /// <summary>
    /// Cria uma campanha ativa, com a descrição padrão.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha ativa {string}")]
    public async Task DadoACampanhaAtiva(string titulo)
    {
        await CriarComoGestorAsync(titulo, "Ajude quem mais precisa.");
    }

    /// <summary>
    /// Cria uma campanha ativa com a descrição informada.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="descricao">Descrição da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha ativa {string} com descrição {string}")]
    public async Task DadoACampanhaAtivaComDescricao(string titulo, string descricao)
    {
        await CriarComoGestorAsync(titulo, descricao);
    }

    /// <summary>
    /// Cria uma campanha e a cancela.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha cancelada {string}")]
    public async Task DadoACampanhaCancelada(string titulo)
    {
        await CriarComoGestorAsync(titulo, "Ajude quem mais precisa.");
        campanhas.AvancarRelogio(TimeSpan.FromSeconds(1));
        await campanhas.AlterarStatusAsync("Cancelada");
        resposta.Resposta!.IsSuccessStatusCode.ShouldBeTrue();
    }

    /// <summary>
    /// Um gestor cria a campanha pela API.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um gestor que cria a campanha {string}")]
    public async Task DadoUmGestorQueCriaACampanha(string titulo)
    {
        await CriarComoGestorAsync(titulo, "Ajude quem mais precisa.");
    }

    /// <summary>
    /// Aponta o Elasticsearch do cenário para um endereço sem servidor.
    /// </summary>
    [Given("o Elasticsearch fora do ar")]
    public void DadoOElasticsearchForaDoAr()
    {
        campanhas.ElasticsearchForaDoAr = true;
    }

    /// <summary>
    /// Busca como visitante, depois de entregar ao índice os eventos que já estão no Outbox.
    /// </summary>
    /// <param name="texto">Texto procurado.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante busca {string}")]
    public async Task QuandoUmVisitanteBusca(string texto)
    {
        await campanhas.EntregarEventosDeBuscaAsync();
        await campanhas.BuscarAsync(texto);
    }

    /// <summary>
    /// Busca como visitante e repete por até o prazo informado, esperando o índice refletir os eventos (SC-003: até 5 segundos).
    /// </summary>
    /// <param name="texto">Texto procurado.</param>
    /// <param name="segundos">Prazo máximo, em segundos.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante busca {string} {int} segundos depois")]
    public async Task QuandoUmVisitanteBuscaSegundosDepois(string texto, int segundos)
    {
        await campanhas.EntregarEventosDeBuscaAsync();
        var limite = DateTime.UtcNow.AddSeconds(segundos);
        do
        {
            await campanhas.BuscarAsync(texto);
            if ((await campanhas.LerTitulosDaBuscaAsync()).Count > 0)
            {
                return;
            }

            await Task.Delay(250);
        }
        while (DateTime.UtcNow < limite);
    }

    /// <summary>
    /// Confere que a campanha está no resultado da busca.
    /// </summary>
    /// <param name="titulo">Título esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("{string} aparece no resultado")]
    public async Task EntaoApareceNoResultado(string titulo)
    {
        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await campanhas.LerTitulosDaBuscaAsync()).ShouldContain(titulo);
    }

    /// <summary>
    /// Confere a ordem de dois títulos no resultado da busca.
    /// </summary>
    /// <param name="primeiro">Título que deve vir antes.</param>
    /// <param name="segundo">Título que deve vir depois.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("{string} aparece antes de {string}")]
    public async Task EntaoApareceAntesDe(string primeiro, string segundo)
    {
        var titulos = await campanhas.LerTitulosDaBuscaAsync();
        titulos.ShouldContain(primeiro);
        titulos.ShouldContain(segundo);
        titulos.ToList().IndexOf(primeiro).ShouldBeLessThan(titulos.ToList().IndexOf(segundo));
    }

    /// <summary>
    /// Confere que a busca não achou nenhuma campanha.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("o resultado é vazio")]
    public async Task EntaoOResultadoEVazio()
    {
        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await campanhas.LerTitulosDaBuscaAsync()).ShouldBeEmpty();
    }

    /// <summary>
    /// Confere que o painel de transparência segue respondendo com o Elasticsearch fora do ar.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("o painel de transparência continua respondendo")]
    public async Task EntaoOPainelContinuaRespondendo()
    {
        await campanhas.ConsultarPainelAsync();
        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Autentica o gestor e cria a campanha pela API, vigente em 10/11/2026 até 24/12/2026.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="descricao">Descrição da campanha.</param>
    /// <returns>Tarefa que termina com a campanha criada.</returns>
    private async Task CriarComoGestorAsync(string titulo, string descricao)
    {
        await campanhas.UsarTokenAsync(await identidade.ObterTokenDoGestorAsync());
        await campanhas.CriarComDescricaoAsync(titulo, descricao);
    }
}
