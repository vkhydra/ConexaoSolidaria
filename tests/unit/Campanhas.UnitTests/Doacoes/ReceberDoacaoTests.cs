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
/// Testes do caso de uso <see cref="ReceberDoacao"/>: aceite com publicação do evento, validação e recusas de negócio.
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

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 150.00m), CancellationToken.None);

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
            Arg.Any<CancellationToken>());
        campanha.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        await _campanhas.DidNotReceive().SalvarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
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

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 1_000_000.00m), CancellationToken.None);

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

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(id, (decimal)valor), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe([campo]);
        await _campanhas.DidNotReceive().ObterPorIdAsync(Arg.Any<CampanhaId>(), Arg.Any<CancellationToken>());
        await _publicador.DidNotReceive().PublicarRecebidaAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dados campos ausentes, quando a doação é recebida, então devolve validacao nos dois campos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_CamposAusentes_DevolveValidacaoNosDoisCampos()
    {
        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(null, null), CancellationToken.None);

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
        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(Guid.NewGuid(), 10m), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
        await _publicador.DidNotReceive().PublicarRecebidaAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<CancellationToken>());
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

        var resultado = await Criar().ExecutarAsync(_doador, new DoacaoRequest(campanha.Id.Valor, 10m), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeDoacao.CodigoCampanhaNaoAtiva);
        await _publicador.DidNotReceive().PublicarRecebidaAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um pedido nulo, quando a doação é recebida, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Receber_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Criar().ExecutarAsync(_doador, null!, CancellationToken.None));
    }

    /// <summary>
    /// Monta o caso de uso com as dependências falsas.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private ReceberDoacao Criar()
    {
        return new ReceberDoacao(_campanhas, _publicador, new DoacaoRequestValidator(), _relogio, NullLogger<ReceberDoacao>.Instance);
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
