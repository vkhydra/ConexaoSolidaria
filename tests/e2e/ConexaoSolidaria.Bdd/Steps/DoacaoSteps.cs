using System.Globalization;
using System.Net;
using ConexaoSolidaria.Bdd.Suporte;
using ConexaoSolidaria.Contracts.V1;
using Reqnroll;

namespace ConexaoSolidaria.Bdd.Steps;

/// <summary>
/// Passos dos cenários da feature 005 (doação) e das partes de outras features que dependem do Worker. As campanhas dos
/// cenários entram direto no banco; o Worker recebe as doações gravadas no Outbox da API por uma ponte que substitui o RabbitMQ.
/// </summary>
/// <param name="campanhas">Ambiente da Campanhas.Api.</param>
/// <param name="worker">Ambiente do Arrecadacao.Worker.</param>
/// <param name="resposta">Última resposta do cenário.</param>
[Binding]
public sealed class DoacaoSteps(AmbienteCampanhas campanhas, AmbienteWorker worker, RespostaDoCenario resposta)
{
    /// <summary>
    /// Data início das campanhas dos cenários, anterior ao "hoje" padrão do relógio do cenário.
    /// </summary>
    private const string _inicio = "2026-11-01";

    /// <summary>
    /// Data fim das campanhas dos cenários, posterior ao "hoje" padrão do relógio do cenário.
    /// </summary>
    private const string _fim = "2026-12-31";

    /// <summary>
    /// Identificador da primeira doação feita com uma chave de idempotência, para comparar com a repetição.
    /// </summary>
    private Guid? _primeiraDoacao;

    /// <summary>
    /// Doação processada pelo Worker no cenário de evento duplicado.
    /// </summary>
    private DoacaoRecebidaEvent? _doacaoProcessada;

    /// <summary>
    /// Total da campanha antes da primeira entrega da doação, para medir o aumento.
    /// </summary>
    private decimal _totalInicial;

    /// <summary>
    /// Cria uma campanha ativa, vigente no "hoje" padrão do cenário.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" ativa")]
    public async Task DadoACampanhaAtiva(string titulo)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", _inicio, _fim, "10000.00", "0.00");
    }

    /// <summary>
    /// Cria uma campanha vigente com o status informado.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="status">Status da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" com status \"(.*)\"")]
    public async Task DadoACampanhaComStatus(string titulo, string status)
    {
        await campanhas.InserirCampanhaAsync(titulo, status, _inicio, _fim, "10000.00", "0.00");
    }

    /// <summary>
    /// Cria uma campanha ativa com o total informado.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="total">Total arrecadado, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" com total (\\d+\\.\\d\\d)")]
    public async Task DadoACampanhaComTotal(string titulo, string total)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", _inicio, _fim, "10000.00", total);
    }

    /// <summary>
    /// Cria uma campanha ativa com o total arrecadado informado (redação usada na feature 004).
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="total">Total arrecadado, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("a campanha \"(.*)\" com total arrecadado (\\d+\\.\\d\\d)")]
    public async Task DadoACampanhaComTotalArrecadado(string titulo, string total)
    {
        await campanhas.InserirCampanhaAsync(titulo, "Ativa", _inicio, _fim, "10000.00", total);
    }

    /// <summary>
    /// Faz uma doação com a chave de idempotência e guarda o identificador dela.
    /// </summary>
    /// <param name="chave">Valor do cabeçalho <c>Idempotency-Key</c>.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("uma doação feita com a chave de idempotência \"(.*)\"")]
    public async Task DadoUmaDoacaoFeitaComAChave(string chave)
    {
        await campanhas.DoarAsync("150.00", chave);

        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _primeiraDoacao = campanhas.UltimaDoacaoId;
    }

    /// <summary>
    /// Entrega ao Worker uma doação de que o total já foi somado.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Given("um \"DoacaoRecebidaEvent\" de (\\d+\\.\\d\\d) já processado")]
    public async Task DadoUmEventoJaProcessado(string valor)
    {
        _totalInicial = await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value);
        _doacaoProcessada = NovaDoacao(campanhas.UltimaCampanhaId!.Value, valor);
        await worker.EntregarAsync(_doacaoProcessada);
    }

    /// <summary>
    /// Doa para a última campanha do cenário.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele doa (\\d+\\.\\d\\d)")]
    public async Task QuandoEleDoa(string valor)
    {
        await campanhas.DoarAsync(valor);
    }

    /// <summary>
    /// Doa para a última campanha do cenário (redação do cenário de valor inválido).
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele doa (\\d+\\.\\d\\d) para uma campanha ativa")]
    public async Task QuandoEleDoaParaUmaCampanhaAtiva(string valor)
    {
        await campanhas.DoarAsync(valor);
    }

    /// <summary>
    /// Doa para uma campanha que não existe.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele doa (\\d+\\.\\d\\d) para uma campanha que não existe")]
    public async Task QuandoEleDoaParaUmaCampanhaQueNaoExiste(string valor)
    {
        await campanhas.DoarParaAsync(Guid.NewGuid(), valor);
    }

    /// <summary>
    /// Tenta doar, sem ter o papel de doador.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele tenta doar (\\d+\\.\\d\\d)")]
    public async Task QuandoEleTentaDoar(string valor)
    {
        await campanhas.DoarParaAsync(campanhas.UltimaCampanhaId ?? Guid.NewGuid(), valor);
    }

    /// <summary>
    /// Repete a doação do cenário com a mesma chave de idempotência e o mesmo corpo.
    /// </summary>
    /// <param name="chave">Valor do cabeçalho <c>Idempotency-Key</c>.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("ele repete a doação com a chave \"(.*)\"")]
    public async Task QuandoEleRepeteADoacao(string chave)
    {
        await campanhas.DoarAsync("150.00", chave);
    }

    /// <summary>
    /// Entrega de novo ao Worker o mesmo evento já processado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [When("o mesmo evento é entregue de novo ao Worker")]
    public async Task QuandoOMesmoEventoEEntregueDeNovo()
    {
        await worker.EntregarAsync(_doacaoProcessada!);
    }

    /// <summary>
    /// Entrega ao Worker uma doação para a campanha informada, como se a API a tivesse aceitado.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [When("uma doação de (\\d+\\.\\d\\d) para \"(.*)\" é processada pelo Worker")]
    public async Task QuandoUmaDoacaoEProcessadaPeloWorker(string valor, string titulo)
    {
        await worker.EntregarAsync(NovaDoacao(campanhas.IdDaCampanha(titulo), valor));
    }

    /// <summary>
    /// Confere que a doação foi aceita (202) e a resposta traz um identificador.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("recebe status 202 com um doacaoId")]
    public async Task EntaoRecebeStatus202ComUmDoacaoId()
    {
        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await campanhas.LerCampoAsync("doacaoId")).ShouldNotBeNullOrEmpty();
        campanhas.UltimaDoacaoId.ShouldNotBeNull();
    }

    /// <summary>
    /// Confere que a repetição foi aceita (202) com o mesmo identificador da primeira doação.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("recebe status 202 com o mesmo doacaoId")]
    public Task EntaoRecebeStatus202ComOMesmoDoacaoId()
    {
        resposta.Resposta!.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        campanhas.UltimaDoacaoId.ShouldBe(_primeiraDoacao);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Confere que o evento de doação recebida está no Outbox da API.
    /// </summary>
    /// <param name="evento">Nome do evento esperado.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o evento \"(.*)\" é publicado")]
    public async Task EntaoOEventoEPublicado(string evento)
    {
        evento.ShouldBe(nameof(DoacaoRecebidaEvent));
        var doacoes = await campanhas.LerDoacoesDoOutboxAsync();

        doacoes.Select(d => d.DoacaoId).ShouldBe([campanhas.UltimaDoacaoId!.Value]);
    }

    /// <summary>
    /// Confere que nenhum evento de doação foi publicado.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("nenhum evento é publicado")]
    public async Task EntaoNenhumEventoEPublicado()
    {
        (await campanhas.LerDoacoesDoOutboxAsync()).ShouldBeEmpty();
    }

    /// <summary>
    /// Confere que só um evento de doação está no Outbox.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("apenas um evento é publicado")]
    public async Task EntaoApenasUmEventoEPublicado()
    {
        (await campanhas.LerDoacoesDoOutboxAsync()).ShouldHaveSingleItem();
    }

    /// <summary>
    /// Confere que a API não alterou o total da campanha.
    /// </summary>
    /// <returns>Tarefa do passo.</returns>
    [Then("o total da campanha não é alterado pela API")]
    public async Task EntaoOTotalNaoEAlteradoPelaApi()
    {
        (await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value)).ShouldBe(0m);
    }

    /// <summary>
    /// Entrega as doações do Outbox ao Worker e espera o total da campanha chegar ao valor, por até o tempo informado.
    /// </summary>
    /// <param name="segundos">Tempo máximo de espera.</param>
    /// <param name="total">Total esperado, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("em até (\\d+) segundos o total da campanha é (\\d+\\.\\d\\d)")]
    public async Task EntaoEmAteSegundosOTotalE(int segundos, string total)
    {
        await worker.EntregarPendentesAsync();
        var esperado = decimal.Parse(total, CultureInfo.InvariantCulture);
        var limite = DateTime.UtcNow.AddSeconds(segundos);
        var atual = await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value);
        while (atual != esperado && DateTime.UtcNow < limite)
        {
            await Task.Delay(100);
            atual = await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value);
        }

        atual.ShouldBe(esperado);
    }

    /// <summary>
    /// Confere o único lançamento do livro de doações da campanha.
    /// </summary>
    /// <param name="valor">Valor esperado do lançamento, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o livro de doações tem um lançamento de (\\d+\\.\\d\\d)")]
    public async Task EntaoOLivroTemUmLancamento(string valor)
    {
        var livro = await worker.LerLivroAsync(campanhas.UltimaCampanhaId!.Value);

        livro.ShouldHaveSingleItem()["valor"].ToDecimal().ShouldBe(decimal.Parse(valor, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Confere que o total da campanha subiu apenas o valor da doação, mesmo com a entrega repetida.
    /// </summary>
    /// <param name="valor">Aumento esperado, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o total da campanha aumenta apenas (\\d+\\.\\d\\d)")]
    public async Task EntaoOTotalAumentaApenas(string valor)
    {
        (await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value)).ShouldBe(_totalInicial + decimal.Parse(valor, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Confere que o total da campanha continua incluindo o valor (por exemplo, depois de o doador anonimizar o cadastro).
    /// </summary>
    /// <param name="valor">Valor que o total deve incluir, com ponto decimal.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("o total da campanha continua incluindo os (\\d+\\.\\d\\d)")]
    public async Task EntaoOTotalContinuaIncluindo(string valor)
    {
        (await campanhas.LerTotalAsync(campanhas.UltimaCampanhaId!.Value)).ShouldBe(decimal.Parse(valor, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Consulta o painel até ele mostrar o total esperado para a campanha, por até o tempo informado.
    /// </summary>
    /// <param name="segundos">Tempo máximo de espera.</param>
    /// <param name="total">Total esperado, com ponto decimal.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>Tarefa do passo.</returns>
    [Then("em até (\\d+) segundos o painel mostra total (\\d+\\.\\d\\d) para \"(.*)\"")]
    public async Task EntaoEmAteSegundosOPainelMostraTotal(int segundos, string total, string titulo)
    {
        var esperado = decimal.Parse(total, CultureInfo.InvariantCulture);
        var limite = DateTime.UtcNow.AddSeconds(segundos);
        decimal? atual;
        do
        {
            await campanhas.ConsultarPainelAsync();
            atual = (await campanhas.LerPainelAsync())
                .Where(i => i.GetProperty("titulo").GetString() == titulo)
                .Select(i => (decimal?)i.GetProperty("totalArrecadado").GetDecimal())
                .SingleOrDefault();
            if (atual != esperado)
            {
                await Task.Delay(200);
            }
        }
        while (atual != esperado && DateTime.UtcNow < limite);

        atual.ShouldBe(esperado);
    }

    /// <summary>
    /// Cria uma doação recebida com identificadores novos para a campanha e o valor informados.
    /// </summary>
    /// <param name="campanhaId">Campanha da doação.</param>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <returns>O evento de doação recebida.</returns>
    private static DoacaoRecebidaEvent NovaDoacao(Guid campanhaId, string valor)
    {
        return new DoacaoRecebidaEvent(Guid.NewGuid(), campanhaId, Guid.NewGuid(), decimal.Parse(valor, CultureInfo.InvariantCulture), new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.FromHours(-3)));
    }
}
