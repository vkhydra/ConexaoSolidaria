using System.Globalization;
using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 004 (painel de transparência), vividos por um visitante sem autenticação.
/// As campanhas dos cenários entram direto no banco, como dados que já existiam.
/// </summary>
/// <param name="campanhas">Ambiente da Campanhas.Api.</param>
/// <param name="resposta">Última resposta do cenário.</param>
[Binding]
public sealed class PainelDeTransparenciaSteps(AmbienteCampanhas campanhas, RespostaDoCenario resposta)
{
    /// <summary>
    /// Cria três campanhas, cada uma com o status do texto do cenário.
    /// </summary>
    /// <param name="a">Título da primeira.</param>
    /// <param name="statusA">Status da primeira.</param>
    /// <param name="b">Título da segunda.</param>
    /// <param name="statusB">Status da segunda.</param>
    /// <param name="c">Título da terceira.</param>
    /// <param name="statusC">Status da terceira.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("as campanhas \"(.*)\" \\((.*)\\), \"(.*)\" \\((.*)\\) e \"(.*)\" \\((.*)\\)")]
    public async Task DadoAsCampanhasComStatus(string a, string statusA, string b, string statusB, string c, string statusC)
    {
        foreach (var (titulo, status) in new[] { (a, statusA), (b, statusB), (c, statusC) })
        {
            await campanhas.InserirCampanhaAsync(titulo, status, "2026-11-10", "2026-12-24", "10000.00", "0.00");
        }
    }

    /// <summary>
    /// Cria uma campanha ativa que termina na data informada.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="fim">Data fim.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" ativa com data fim \"(.*)\"")]
    public async Task DadoACampanhaAtivaComDataFim(string titulo, string fim)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", "2026-11-10", fim, "10000.00", "0.00");
    }

    /// <summary>
    /// Cria uma campanha ativa que só começa na data informada.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="inicio">Data início.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" ativa com data início \"(.*)\"")]
    public async Task DadoACampanhaAtivaComDataInicio(string titulo, string inicio)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", inicio, "2027-01-31", "10000.00", "0.00");
    }

    /// <summary>
    /// Cria uma campanha ativa com meta e total arrecadado definidos.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="meta">Meta em reais.</param>
    /// <param name="total">Total arrecadado em reais.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" com meta (.*) e total arrecadado (.*)")]
    public async Task DadoACampanhaComMetaETotal(string titulo, string meta, string total)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", "2026-11-10", "2026-12-24", meta, total);
    }

    /// <summary>
    /// Aponta o Redis do cenário para um endereço sem servidor.
    /// </summary>
    [Given("o Redis fora do ar")]
    public void DadoORedisForaDoAr()
    {
        campanhas.RedisForaDoAr = true;
    }

    /// <summary>
    /// Consulta o painel como visitante.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("um visitante consulta o painel")]
    public async Task QuandoUmVisitanteConsultaOPainel()
    {
        await campanhas.ConsultarPainelAsync();
    }

    /// <summary>
    /// Confere que o painel mostra só a campanha informada.
    /// </summary>
    /// <param name="titulo">Título esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("vê apenas a campanha \"(.*)\"")]
    public async Task EntaoVeApenasACampanha(string titulo)
    {
        (await campanhas.LerPainelAsync()).Select(i => i.GetProperty("titulo").GetString()).ShouldBe([titulo]);
    }

    /// <summary>
    /// Confere que a campanha não aparece no painel.
    /// </summary>
    /// <param name="titulo">Título que não deve aparecer.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("a campanha \"(.*)\" não aparece")]
    public async Task EntaoACampanhaNaoAparece(string titulo)
    {
        (await campanhas.LerPainelAsync()).Select(i => i.GetProperty("titulo").GetString()).ShouldNotContain(titulo);
    }

    /// <summary>
    /// Confere o percentual atingido de uma campanha do painel.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="percentual">Percentual esperado, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o percentual atingido de \"(.*)\" é (.*)")]
    public async Task EntaoOPercentualAtingidoE(string titulo, string percentual)
    {
        var item = (await campanhas.LerPainelAsync()).Single(i => i.GetProperty("titulo").GetString() == titulo);

        item.GetProperty("percentualAtingido").GetDecimal().ShouldBe(decimal.Parse(percentual, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Confere que o painel respondeu com o status esperado e trouxe os dados do banco (ao menos uma campanha).
    /// </summary>
    /// <param name="status">Status esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("recebe status (.*) com os dados do banco")]
    public async Task EntaoRecebeStatusComOsDadosDoBanco(int status)
    {
        resposta.Resposta!.StatusCode.ShouldBe((HttpStatusCode)status);
        (await campanhas.LerPainelAsync()).ShouldNotBeEmpty();
    }
}
