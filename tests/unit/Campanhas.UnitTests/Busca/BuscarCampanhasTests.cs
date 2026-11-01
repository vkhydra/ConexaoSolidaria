using Campanhas.Application.Busca;
using Campanhas.Application.Comum;

namespace Campanhas.UnitTests.Busca;

/// <summary>
/// Testes do caso de uso <see cref="BuscarCampanhas"/>: validação do texto e da paginação, paginação do resultado e
/// busca indisponível.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BuscarCampanhasTests
{
    /// <summary>
    /// Porta de busca falsa.
    /// </summary>
    private readonly IBuscaCampanhas _busca = Substitute.For<IBuscaCampanhas>();

    /// <summary>
    /// Métricas falsas.
    /// </summary>
    private readonly IMetricasBusca _metricas = Substitute.For<IMetricasBusca>();

    /// <summary>
    /// Caso de uso sob teste, com o validador real.
    /// </summary>
    private readonly BuscarCampanhas _caso;

    /// <summary>
    /// Monta o caso de uso.
    /// </summary>
    public BuscarCampanhasTests()
    {
        _caso = new BuscarCampanhas(_busca, new BuscarCampanhasRequestValidator(), _metricas);
    }

    /// <summary>
    /// Dado um texto válido, quando a busca acha campanhas, então a resposta traz os itens e calcula as páginas.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_TextoValido_DevolveItensEPaginas()
    {
        var item = new BuscaItemResponse(Guid.NewGuid(), "Natal Solidário", "Presentes e <em>ceia</em>", 3.4, new DateOnly(2026, 12, 24));
        _busca.BuscarAsync("natl solidaro", 2, 10, Arg.Any<CancellationToken>())
            .Returns(new Pagina<BuscaItemResponse>([item], 2, 10, 11));

        var resultado = await _caso.ExecutarAsync(new BuscarCampanhasRequest("  natl solidaro  ", 2, 10), TestContext.Current.CancellationToken);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Items.ShouldBe([item]);
        resultado.Valor.Page.ShouldBe(2);
        resultado.Valor.PageSize.ShouldBe(10);
        resultado.Valor.TotalItems.ShouldBe(11);
        resultado.Valor.TotalPages.ShouldBe(2);
        _metricas.Received(1).RegistrarBusca(ResultadoDaBusca.Ok);
    }

    /// <summary>
    /// Dado um texto que não casa com nada, quando a busca roda, então o resultado é uma página vazia com zero páginas.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_SemResultados_DevolvePaginaVazia()
    {
        _busca.BuscarAsync("de", 1, 10, Arg.Any<CancellationToken>()).Returns(new Pagina<BuscaItemResponse>([], 1, 10, 0));

        var resultado = await _caso.ExecutarAsync(new BuscarCampanhasRequest("de"), TestContext.Current.CancellationToken);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Items.ShouldBeEmpty();
        resultado.Valor.TotalPages.ShouldBe(0);
        _metricas.Received(1).RegistrarBusca(ResultadoDaBusca.Vazio);
    }

    /// <summary>
    /// Dado um pedido fora das regras, quando executado, então devolve <c>validacao</c> sem consultar o índice.
    /// </summary>
    /// <param name="q">Texto procurado.</param>
    /// <param name="page">Página.</param>
    /// <param name="pageSize">Tamanho da página.</param>
    /// <param name="campo">Campo que deve aparecer no erro.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData(null, 1, 10, "q")]
    [InlineData("", 1, 10, "q")]
    [InlineData("a", 1, 10, "q")]
    [InlineData(" a ", 1, 10, "q")]
    [InlineData("natal", 0, 10, "page")]
    [InlineData("natal", 1, 0, "pageSize")]
    [InlineData("natal", 1, 51, "pageSize")]
    public async Task Executar_PedidoInvalido_DevolveValidacao(string? q, int page, int pageSize, string campo)
    {
        var resultado = await _caso.ExecutarAsync(new BuscarCampanhasRequest(q, page, pageSize), TestContext.Current.CancellationToken);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.ShouldContainKey(campo);
        await _busca.DidNotReceiveWithAnyArgs().BuscarAsync(default!, default, default, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Dado um texto no limite de 100 caracteres, quando executado, então é aceito; com 101, é recusado.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_LimiteDoTamanhoDoTexto_AceitaCemERecusaCentoEUm()
    {
        _busca.BuscarAsync(Arg.Any<string>(), 1, 10, Arg.Any<CancellationToken>()).Returns(new Pagina<BuscaItemResponse>([], 1, 10, 0));

        var cem = await _caso.ExecutarAsync(new BuscarCampanhasRequest(new string('a', 100)), TestContext.Current.CancellationToken);
        var centoEUm = await _caso.ExecutarAsync(new BuscarCampanhasRequest(new string('a', 101)), TestContext.Current.CancellationToken);

        cem.EhSucesso.ShouldBeTrue();
        centoEUm.EhFalha.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um Elasticsearch fora do ar, quando a busca roda, então o erro é <c>busca.indisponivel</c>.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_BuscaIndisponivel_DevolveErroDeIndisponibilidade()
    {
        _busca.BuscarAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Pagina<BuscaItemResponse>>(_ => throw new BuscaIndisponivelException());

        var resultado = await _caso.ExecutarAsync(new BuscarCampanhasRequest("natal"), TestContext.Current.CancellationToken);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(ErrosDeBusca.CodigoIndisponivel);
        _metricas.Received(1).RegistrarBusca(ResultadoDaBusca.Indisponivel);
    }

    /// <summary>
    /// Dado um pedido nulo, quando executado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Executar_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _caso.ExecutarAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dados os três construtores, quando a exceção é criada, então mensagem e causa são guardadas.
    /// </summary>
    [Fact]
    public void BuscaIndisponivelException_Construtores_GuardamMensagemECausa()
    {
        var causa = new InvalidOperationException("causa");

        new BuscaIndisponivelException().Message.ShouldNotBeNullOrEmpty();
        new BuscaIndisponivelException("fora do ar").Message.ShouldBe("fora do ar");
        new BuscaIndisponivelException("fora do ar", causa).InnerException.ShouldBeSameAs(causa);
    }
}
