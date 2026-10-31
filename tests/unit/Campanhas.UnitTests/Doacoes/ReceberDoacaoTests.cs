using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Application.Doacoes;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.Logging.Abstractions;

namespace Campanhas.UnitTests.Doacoes;

/// <summary>
/// Testes do caso de uso <see cref="ReceberDoacao"/>: aceite com publicação do evento, validação, recusas de negócio e
/// chave de idempotência.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReceberDoacaoTests
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Instante atual usado nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 10, 5, TimeSpan.FromHours(-3));

    /// <summary>
    /// Doador autenticado dos cenários.
    /// </summary>
    private static readonly Guid _doador = Guid.Parse("0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70");

    /// <summary>
    /// Repositório de campanhas falso.
    /// </summary>
    private readonly ICampanhaRepositorio _campanhas = Substitute.For<ICampanhaRepositorio>();

    /// <summary>
    /// Porta de publicação das doações falsa.
    /// </summary>
    private readonly IPublicadorDeDoacoes _publicador = Substitute.For<IPublicadorDeDoacoes>();

    /// <summary>
    /// Porta das chaves de idempotência falsa.
    /// </summary>
    private readonly IChavesIdempotencia _chaves = Substitute.For<IChavesIdempotencia>();

    /// <summary>
    /// Relógio falso fixo em <see cref="_agora"/>.
    /// </summary>
    private readonly IRelogio _relogio = Substitute.For<IRelogio>();

    /// <summary>
    /// Prepara o relógio.
    /// </summary>
    public ReceberDoacaoTests()
    {
        _relogio.Hoje().Returns(_hoje);
        _relogio.Agora().Returns(_agora);
    }

    /// <summary>
    /// Dada uma campanha ativa, quando a doação é recebida, então devolve o aceite, publica o evento só com identificadores
    /// e não altera o total arrecadado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CampanhaAtiva_AceitaEPublicaEvento()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150.00m), null, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Status.ShouldBe("Recebida");
        resultado.Valor.DoacaoId.ShouldNotBe(Guid.Empty);
        resultado.Valor.Mensagem.ShouldBe("Doação recebida. O total será atualizado em instantes.");
        await _publicador.Received(1).PublicarRecebidaAsync(
            Arg.Is<DoacaoRecebidaEvent>(e =>
                e.DoacaoId == resultado.Valor.DoacaoId
                && e.CampanhaId == campanha.Id.Valor
                && e.DoadorId == _doador
                && e.Valor == 150.00m
                && e.OcorridoEm == _agora),
            null,
            Arg.Any<CancellationToken>());
        campanha.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        await _campanhas.DidNotReceive().SalvarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
        await _chaves.DidNotReceive().ObterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado o maior valor aceito (1.000.000,00), quando a doação é recebida, então é aceita.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ValorNoLimiteMaximo_Aceita()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 1_000_000.00m), null, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um pedido fora do formato, quando a doação é recebida, então devolve validacao com o campo certo e nada é consultado nem publicado.
    /// </summary>
    /// <param name="campo">Campo esperado no erro.</param>
    /// <param name="idCampanha">Identificador enviado (vazio para o caso de ausência).</param>
    /// <param name="valor">Valor enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("idCampanha", "", 10.00)]
    [InlineData("valorDoacao", "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", 0.00)]
    [InlineData("valorDoacao", "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", -5.00)]
    [InlineData("valorDoacao", "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", 10.005)]
    [InlineData("valorDoacao", "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", 1_000_000.01)]
    public async Task Receber_PedidoInvalido_DevolveValidacao(string campo, string idCampanha, double valor)
    {
        var id = idCampanha.Length == 0 ? Guid.Empty : Guid.Parse(idCampanha);

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(id, (decimal)valor), null, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe([campo]);
        await _campanhas.DidNotReceive().ObterPorIdAsync(Arg.Any<CampanhaId>(), Arg.Any<CancellationToken>());
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Dados campos ausentes, quando a doação é recebida, então devolve validacao nos dois campos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CamposAusentes_DevolveValidacaoNosDoisCampos()
    {
        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(null, null), null, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe(["idCampanha", "valorDoacao"], ignoreOrder: true);
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando a doação é recebida, então devolve campanha.nao_encontrada e nada é publicado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CampanhaInexistente_DevolveNaoEncontrada()
    {
        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(Guid.NewGuid(), 10m), null, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Dada uma campanha concluída, cancelada ou fora do período, quando a doação é recebida, então devolve
    /// doacao.campanha_nao_ativa e nada é publicado.
    /// </summary>
    /// <param name="situacao">Situação que torna a campanha inapta.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("concluida")]
    [InlineData("cancelada")]
    [InlineData("antes-do-inicio")]
    [InlineData("depois-do-fim")]
    public async Task Receber_CampanhaInapta_DevolveCampanhaNaoAtiva(string situacao)
    {
        var campanha = NovaCampanha();
        switch (situacao)
        {
            case "concluida":
                campanha.Concluir(_agora);
                break;
            case "cancelada":
                campanha.Cancelar(_agora);
                break;
            case "antes-do-inicio":
                _relogio.Hoje().Returns(_hoje.AddDays(-1));
                break;
            default:
                _relogio.Hoje().Returns(new DateOnly(2026, 12, 25));
                break;
        }

        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 10m), null, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeDoacao.CodigoCampanhaNaoAtiva);
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Dado um pedido nulo, quando a doação é recebida, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Criar().ExecutarAsync(_doador, null!, null, CancellationToken.None));
    }

    /// <summary>
    /// Dada uma chave de idempotência nova, quando a doação é recebida, então o publicador recebe a chave com o hash do corpo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveNova_PublicaComAChave()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150m), "chave-1", CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        await _publicador.Received(1).PublicarRecebidaAsync(
            Arg.Any<DoacaoRecebidaEvent>(),
            Arg.Is<ChaveParaRegistrar?>(c => c != null && c.Chave == "chave-1" && c.HashCorpo.SequenceEqual(Hash(campanha.Id.Valor, 150m))),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma chave já usada com o mesmo corpo, quando a doação é repetida, então devolve o mesmo aceite e não publica de novo,
    /// mesmo que a campanha tenha encerrado nesse meio-tempo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveRepetidaComMesmoCorpo_DevolveAMesmaDoacao()
    {
        var campanha = NovaCampanha();
        campanha.Cancelar(_agora);
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);
        var existente = Guid.NewGuid();
        _chaves.ObterAsync("chave-1", _doador, Arg.Any<CancellationToken>()).Returns(new ChaveRegistrada(existente, Hash(campanha.Id.Valor, 150m)));

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150m), "chave-1", CancellationToken.None);

        resultado.Valor.DoacaoId.ShouldBe(existente);
        resultado.Valor.Status.ShouldBe("Recebida");
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Dada uma chave já usada com outro corpo, quando a doação é repetida, então devolve doacao.chave_idempotencia_conflitante.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveRepetidaComOutroCorpo_DevolveConflito()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);
        _chaves.ObterAsync("chave-1", _doador, Arg.Any<CancellationToken>()).Returns(new ChaveRegistrada(Guid.NewGuid(), Hash(campanha.Id.Valor, 99m)));

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150m), "chave-1", CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeDoacao.CodigoChaveConflitante);
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Dado que outra requisição registrou a mesma chave no mesmo instante, quando a publicação falha por duplicidade,
    /// então o caso de uso relê a chave e devolve a doação da outra requisição.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_ChaveGravadaEmParalelo_DevolveADoacaoVencedora()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);
        var vencedora = Guid.NewGuid();
        _chaves.ObterAsync("chave-1", _doador, Arg.Any<CancellationToken>())
            .Returns(null, new ChaveRegistrada(vencedora, Hash(campanha.Id.Valor, 150m)));
        _publicador.PublicarRecebidaAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<ChaveParaRegistrar?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ChaveJaRegistradaException()));

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150m), "chave-1", CancellationToken.None);

        resultado.Valor.DoacaoId.ShouldBe(vencedora);
    }

    /// <summary>
    /// Dada uma chave vazia, só com espaços ou com mais de 100 caracteres, quando a doação é recebida, então devolve validacao
    /// no cabeçalho Idempotency-Key e nada é consultado nem publicado.
    /// </summary>
    /// <param name="chave">Chave enviada.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
    public async Task Receber_ChaveInvalida_DevolveValidacao(string chave)
    {
        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(Guid.NewGuid(), 10m), chave, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe(["Idempotency-Key"]);
        await _chaves.DidNotReceive().ObterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await NadaPublicadoAsync();
    }

    /// <summary>
    /// Confere que nenhum evento de doação foi publicado.
    /// </summary>
    /// <returns>Tarefa que termina com a conferência feita.</returns>
    private async Task NadaPublicadoAsync()
    {
        await _publicador.DidNotReceive().PublicarRecebidaAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<ChaveParaRegistrar?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Calcula o hash do corpo da mesma forma que o caso de uso, para montar chaves já registradas.
    /// </summary>
    /// <param name="campanhaId">Campanha do pedido.</param>
    /// <param name="valor">Valor do pedido.</param>
    /// <returns>O SHA-256 do corpo canônico.</returns>
    private static byte[] Hash(Guid campanhaId, decimal valor)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{campanhaId:D}|{valor:F2}")));
    }

    /// <summary>
    /// Monta o caso de uso com as dependências falsas.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private ReceberDoacao Criar()
    {
        return new ReceberDoacao(_campanhas, _publicador, _chaves, new DoacaoRequestValidator(), _relogio, NullLogger<ReceberDoacao>.Instance);
    }

    /// <summary>
    /// Cria uma campanha ativa de 10000.00, de 10/11 a 24/12.
    /// </summary>
    /// <returns>A campanha criada.</returns>
    private static Campanha NovaCampanha()
    {
        return Campanha.Criar("Natal Solidário", "Cestas", Periodo.Criar(_hoje, new DateOnly(2026, 12, 24)).Valor, Dinheiro.Criar(10000m).Valor, _hoje, _agora).Valor;
    }
}
