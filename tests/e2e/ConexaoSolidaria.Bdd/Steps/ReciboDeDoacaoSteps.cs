using System.Globalization;
using ConexaoSolidaria.Bdd.Suporte;
using ConexaoSolidaria.Contracts.V1;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 007 (recibo de doação). O Worker processa a doação de verdade e o envelope que o Outbox
/// entrega é passado à função de recibo (no lugar do gatilho RabbitMQ); o recibo é conferido no log capturado. Os cenários de
/// escala (KEDA) rodam no Kubernetes, não aqui.
/// </summary>
/// <param name="campanhas">Ambiente da Campanhas.Api, de onde vêm o banco e as campanhas.</param>
/// <param name="worker">Ambiente do Arrecadacao.Worker.</param>
/// <param name="recibo">Ambiente da Notificacoes.Function.</param>
[Binding]
public sealed class ReciboDeDoacaoSteps(AmbienteCampanhas campanhas, AmbienteWorker worker, AmbienteRecibo recibo)
{
    /// <summary>
    /// Título da campanha criada para os cenários de recibo.
    /// </summary>
    private const string _tituloDaCampanha = "Campanha do recibo";

    /// <summary>
    /// Doações processadas no cenário, por rótulo ("D1"); a sem rótulo usa a chave vazia.
    /// </summary>
    private readonly Dictionary<string, (Guid DoacaoId, string Envelope)> _doacoes = [];

    /// <summary>
    /// Processa uma doação pelo Worker e entrega o evento processado à função de recibo.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("uma doação de (\\d+\\.\\d\\d) é processada")]
    public async Task QuandoUmaDoacaoEProcessada(string valor)
    {
        await ProcessarEReceberAsync(string.Empty, valor);
    }

    /// <summary>
    /// Pré-condição: a doação já foi processada e a função já emitiu o recibo.
    /// </summary>
    /// <param name="rotulo">Rótulo da doação.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um recibo já emitido para a doação {string}")]
    public async Task DadoUmReciboJaEmitido(string rotulo)
    {
        await ProcessarEReceberAsync(rotulo, "150.00");
    }

    /// <summary>
    /// A fila entrega de novo o mesmo evento à função.
    /// </summary>
    /// <param name="rotulo">Rótulo da doação.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("o evento da doação {string} é entregue de novo")]
    public async Task QuandoOEventoEEntregueDeNovo(string rotulo)
    {
        await recibo.ReceberAsync(_doacoes[rotulo].Envelope);
    }

    /// <summary>
    /// Confere que a função registrou o recibo com o identificador da doação e o valor.
    /// </summary>
    /// <param name="valor">Valor esperado, com ponto decimal.</param>
    [Then("a função registra \"ReciboEmitido\" com o doacaoId e o valor (\\d+\\.\\d\\d)")]
    public void EntaoAFuncaoRegistraOReciboEmitido(string valor)
    {
        var registro = recibo.RecibosDe(_doacoes[string.Empty].DoacaoId).ShouldHaveSingleItem();
        registro.Campos["Valor"].ShouldBe(decimal.Parse(valor, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Confere que o registro do recibo não tem dado pessoal.
    /// </summary>
    [Then("o registro não contém nome, e-mail nem CPF")]
    public void EntaoORegistroNaoContemDadosPessoais()
    {
        var registro = recibo.RecibosDe(_doacoes[string.Empty].DoacaoId).ShouldHaveSingleItem();
        AmbienteRecibo.ProblemasDeDadosPessoais(registro).ShouldBeEmpty();
    }

    /// <summary>
    /// Confere que a entrega repetida não gerou um segundo recibo.
    /// </summary>
    /// <param name="evento">Nome do evento de log (<c>ReciboEmitido</c>).</param>
    /// <param name="rotulo">Rótulo da doação.</param>
    [Then("nenhum novo {string} é registrado para {string}")]
    public void EntaoNenhumNovoReciboERegistrado(string evento, string rotulo)
    {
        evento.ShouldBe("ReciboEmitido");
        recibo.RecibosDe(_doacoes[rotulo].DoacaoId).Count.ShouldBe(1);
    }

    /// <summary>
    /// Cria a campanha, processa a doação no Worker e entrega à função o envelope que o Outbox gerou.
    /// </summary>
    /// <param name="rotulo">Rótulo da doação.</param>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa que termina com o recibo emitido.</returns>
    private async Task ProcessarEReceberAsync(string rotulo, string valor)
    {
        await campanhas.InserirCampanhaAsync(_tituloDaCampanha, "Ativa", "2026-11-10", "2026-12-24", "10000.00", "0.00");
        var doacao = new DoacaoRecebidaEvent(Guid.NewGuid(), campanhas.IdDaCampanha(_tituloDaCampanha), Guid.NewGuid(), decimal.Parse(valor, CultureInfo.InvariantCulture), new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.FromHours(-3)));
        await worker.EntregarAsync(doacao);
        var envelope = await worker.ObterEnvelopeProcessadoAsync(doacao.DoacaoId);
        _doacoes[rotulo] = (doacao.DoacaoId, envelope);
        await recibo.ReceberAsync(envelope);
    }
}
