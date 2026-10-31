using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.UnitTests.Suporte;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes do caso de uso de criação de campanha: sucesso, validação (400) e regras de negócio (422).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CriarCampanhaTests
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Repositório de campanhas falso.
    /// </summary>
    private readonly ICampanhaRepositorio _campanhas = Substitute.For<ICampanhaRepositorio>();

    /// <summary>
    /// Relógio falso fixado em hoje.
    /// </summary>
    private readonly IRelogio _relogio = Substitute.For<IRelogio>();

    /// <summary>
    /// Métricas de campanhas falsas.
    /// </summary>
    private readonly IMetricasCampanhas _metricas = Substitute.For<IMetricasCampanhas>();

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<CriarCampanha> _log = new();

    /// <summary>
    /// Caso de uso sob teste.
    /// </summary>
    private readonly CriarCampanha _criar;

    /// <summary>
    /// Prepara o relógio e o caso de uso.
    /// </summary>
    public CriarCampanhaTests()
    {
        _relogio.Hoje().Returns(_hoje);
        _relogio.Agora().Returns(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3)));
        _criar = new CriarCampanha(_campanhas, new CampanhaRequestValidator(), _relogio, _metricas, _log);
    }

    /// <summary>
    /// Dados válidos, quando o gestor cria a campanha, então ela é gravada ativa, com total zero e percentual zero.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_DadosValidos_GravaCampanhaAtiva()
    {
        var resultado = await _criar.ExecutarAsync(Pedido(), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Titulo.ShouldBe("Natal Solidário");
        resultado.Valor.Status.ShouldBe("Ativa");
        resultado.Valor.MetaFinanceira.ShouldBe(10000m);
        resultado.Valor.TotalArrecadado.ShouldBe(0m);
        resultado.Valor.PercentualAtingido.ShouldBe(0m);
        resultado.Valor.DataInicio.ShouldBe(_hoje);
        await _campanhas.Received(1).AdicionarAsync(
            Arg.Is<Campanha>(c => c.Id.Valor == resultado.Valor.Id && c.Status == StatusCampanha.Ativa),
            Arg.Any<CancellationToken>());
        _log.Mensagens.ShouldHaveSingleItem().ShouldBe($"Campanha criada: {resultado.Valor.Id}.");
        _metricas.Received(1).RegistrarCriacao();
    }

    /// <summary>
    /// Dada uma descrição ausente, quando a campanha é criada, então é tratada como vazia.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_DescricaoAusente_GravaComoVazia()
    {
        var resultado = await _criar.ExecutarAsync(Pedido(descricao: null), CancellationToken.None);

        resultado.Valor.Descricao.ShouldBeEmpty();
    }

    /// <summary>
    /// Dados com formato inválido, quando enviados, então devolve validacao com o campo certo e nada é gravado.
    /// </summary>
    /// <param name="campo">Campo esperado no erro.</param>
    /// <param name="titulo">Título enviado.</param>
    /// <param name="meta">Meta enviada.</param>
    /// <param name="comDatas">Se as datas são enviadas.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("titulo", null, 100.0, true)]
    [InlineData("titulo", "ab", 100.0, true)]
    [InlineData("titulo", " Natal", 100.0, true)]
    [InlineData("titulo", "Natal ", 100.0, true)]
    [InlineData("metaFinanceira", "Natal", null, true)]
    [InlineData("metaFinanceira", "Natal", 10.123, true)]
    [InlineData("dataInicio", "Natal", 100.0, false)]
    public async Task ExecutarAsync_FormatoInvalido_DevolveValidacao(string campo, string? titulo, double? meta, bool comDatas)
    {
        var request = new CampanhaRequest(titulo, string.Empty, comDatas ? _hoje : null, comDatas ? _hoje : null, meta is null ? null : (decimal)meta);

        var resultado = await _criar.ExecutarAsync(request, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldContain(campo);
        await _campanhas.DidNotReceive().AdicionarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um pedido sem nenhum campo, quando enviado, então todos os campos obrigatórios aparecem no erro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_PedidoVazio_ListaTodosOsCamposObrigatorios()
    {
        var resultado = await _criar.ExecutarAsync(new CampanhaRequest(null, null, null, null, null), CancellationToken.None);

        resultado.Erro.Campos!.Keys.ShouldBe(["titulo", "dataInicio", "dataFim", "metaFinanceira"], ignoreOrder: true);
    }

    /// <summary>
    /// Dada uma descrição com mais de 2000 caracteres, quando enviada, então devolve validacao no campo descricao.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_DescricaoGrande_DevolveValidacao()
    {
        var resultado = await _criar.ExecutarAsync(Pedido(descricao: new string('a', 2001)), CancellationToken.None);

        resultado.Erro.Campos!.Keys.ShouldBe(["descricao"]);
    }

    /// <summary>
    /// Dadas regras de negócio violadas (data fim passada, meta zero ou negativa, período invertido), quando enviadas, então devolve o código do domínio.
    /// </summary>
    /// <param name="inicio">Data início.</param>
    /// <param name="fim">Data fim.</param>
    /// <param name="meta">Meta.</param>
    /// <param name="codigo">Código esperado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("2026-11-01", "2026-11-09", 1000, "campanha.data_fim_passada")]
    [InlineData("2026-11-10", "2026-12-01", 0, "campanha.meta_invalida")]
    [InlineData("2026-11-10", "2026-12-01", -10, "campanha.meta_invalida")]
    [InlineData("2026-12-10", "2026-12-01", 1000, "campanha.periodo_invalido")]
    public async Task ExecutarAsync_RegraDeNegocioViolada_DevolveCodigoDoDominio(string inicio, string fim, double meta, string codigo)
    {
        var request = new CampanhaRequest("Natal", string.Empty, DateOnly.Parse(inicio, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(fim, System.Globalization.CultureInfo.InvariantCulture), (decimal)meta);

        var resultado = await _criar.ExecutarAsync(request, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(codigo);
        await _campanhas.DidNotReceive().AdicionarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
        _metricas.DidNotReceive().RegistrarCriacao();
    }

    /// <summary>
    /// Dado um pedido nulo, quando enviado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _criar.ExecutarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Dado um resultado de validação nulo, quando convertido, então a chamada é recusada; o nome vazio não quebra a conversão.
    /// </summary>
    [Fact]
    public void ErrosDeValidacao_ResultadoNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => ErrosDeValidacao.Converter(null!));
        ErrosDeValidacao.Converter(new FluentValidation.Results.ValidationResult([new FluentValidation.Results.ValidationFailure(string.Empty, "x")])).Campos.ShouldNotBeNull();
    }

    /// <summary>
    /// Monta um pedido válido, trocando só o que o teste precisa.
    /// </summary>
    /// <param name="descricao">Descrição do pedido.</param>
    /// <returns>O pedido.</returns>
    private static CampanhaRequest Pedido(string? descricao = "Cestas de Natal")
    {
        return new CampanhaRequest("Natal Solidário", descricao, _hoje, new DateOnly(2026, 12, 24), 10000.00m);
    }
}
