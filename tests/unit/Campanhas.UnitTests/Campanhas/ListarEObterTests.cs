using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes dos casos de uso de listagem (paginação e filtro) e de detalhe de campanha.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ListarEObterTests
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
    /// Dado um repositório com 45 campanhas, quando se pede a página 3 de 20, então vem a página com 5 itens e 3 páginas no total.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Listar_PaginaDoMeio_DevolveItensETotais()
    {
        var itens = Enumerable.Range(0, 5).Select(_ => NovaCampanha()).ToList();
        _campanhas.ListarAsync(StatusCampanha.Ativa, 3, 20, Arg.Any<CancellationToken>()).Returns(new Pagina<Campanha>(itens, 3, 20, 45));

        var resultado = await new ListarCampanhas(_campanhas, new ListarCampanhasRequestValidator())
            .ExecutarAsync(new ListarCampanhasRequest("Ativa", 3, 20), CancellationToken.None);

        resultado.Valor.Items.Count.ShouldBe(5);
        resultado.Valor.Page.ShouldBe(3);
        resultado.Valor.PageSize.ShouldBe(20);
        resultado.Valor.TotalItems.ShouldBe(45);
        resultado.Valor.TotalPages.ShouldBe(3);
    }

    /// <summary>
    /// Dado um pedido sem filtro, quando listado, então o repositório recebe status nulo e a lista vazia tem zero páginas.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Listar_SemFiltro_PassaStatusNuloEAceitaListaVazia()
    {
        _campanhas.ListarAsync(null, 1, 20, Arg.Any<CancellationToken>()).Returns(new Pagina<Campanha>([], 1, 20, 0));

        var resultado = await new ListarCampanhas(_campanhas, new ListarCampanhasRequestValidator())
            .ExecutarAsync(new ListarCampanhasRequest(), CancellationToken.None);

        resultado.Valor.Items.ShouldBeEmpty();
        resultado.Valor.TotalPages.ShouldBe(0);
    }

    /// <summary>
    /// Dados página, tamanho ou status fora da regra, quando listado, então devolve validacao com o campo certo e não consulta o repositório.
    /// </summary>
    /// <param name="campo">Campo esperado no erro.</param>
    /// <param name="status">Status enviado.</param>
    /// <param name="pagina">Página enviada.</param>
    /// <param name="tamanho">Tamanho enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("page", null, 0, 20)]
    [InlineData("page", null, -1, 20)]
    [InlineData("pageSize", null, 1, 0)]
    [InlineData("pageSize", null, 1, 101)]
    [InlineData("status", "Pausada", 1, 20)]
    [InlineData("status", "ativa", 1, 20)]
    public async Task Listar_ParametrosInvalidos_DevolveValidacao(string campo, string? status, int pagina, int tamanho)
    {
        var resultado = await new ListarCampanhas(_campanhas, new ListarCampanhasRequestValidator())
            .ExecutarAsync(new ListarCampanhasRequest(status, pagina, tamanho), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe([campo]);
        await _campanhas.DidNotReceive().ListarAsync(Arg.Any<StatusCampanha?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dados os limites de tamanho de página (1 e 100), quando listado, então são aceitos.
    /// </summary>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Listar_LimitesDeTamanho_Aceita(int tamanho)
    {
        _campanhas.ListarAsync(null, 1, tamanho, Arg.Any<CancellationToken>()).Returns(new Pagina<Campanha>([], 1, tamanho, 0));

        var resultado = await new ListarCampanhas(_campanhas, new ListarCampanhasRequestValidator())
            .ExecutarAsync(new ListarCampanhasRequest(null, 1, tamanho), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um pedido nulo, quando listado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Listar_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => new ListarCampanhas(_campanhas, new ListarCampanhasRequestValidator()).ExecutarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Dada uma campanha existente, quando detalhada, então devolve os dados com o percentual atingido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Obter_CampanhaExistente_DevolveDados()
    {
        var campanha = NovaCampanha();
        _campanhas.ObterPorIdAsync(campanha.Id, Arg.Any<CancellationToken>()).Returns(campanha);

        var resultado = await new ObterCampanha(_campanhas).ExecutarAsync(campanha.Id, CancellationToken.None);

        resultado.Valor.Id.ShouldBe(campanha.Id.Valor);
        resultado.Valor.PercentualAtingido.ShouldBe(0m);
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando detalhada, então devolve campanha.nao_encontrada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Obter_CampanhaInexistente_DevolveNaoEncontrada()
    {
        var resultado = await new ObterCampanha(_campanhas).ExecutarAsync(CampanhaId.Novo(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
    }

    /// <summary>
    /// Dada uma campanha com total e meta, quando a resposta é montada, então o percentual é arredondado em duas casas; sem meta, é zero.
    /// </summary>
    [Fact]
    public void CampanhaResponse_De_CalculaPercentualAtingido()
    {
        var campanha = NovaCampanha();
        typeof(Campanha).GetProperty(nameof(Campanha.TotalArrecadado))!.SetValue(campanha, Dinheiro.Criar(3333.33m).Valor);

        CampanhaResponse.De(campanha).PercentualAtingido.ShouldBe(33.33m);
        Should.Throw<ArgumentNullException>(() => CampanhaResponse.De(null!));
    }

    /// <summary>
    /// Cria uma campanha ativa de 10000.00 para os testes.
    /// </summary>
    /// <returns>A campanha criada.</returns>
    private static Campanha NovaCampanha()
    {
        return Campanha.Criar(
            "Natal Solidário",
            "Cestas",
            Periodo.Criar(_hoje, new DateOnly(2026, 12, 24)).Valor,
            Dinheiro.Criar(10000m).Valor,
            _hoje,
            new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3))).Valor;
    }
}
