using Campanhas.Application.Busca;
using Microsoft.Extensions.Logging.Abstractions;

namespace Campanhas.UnitTests.Busca;

/// <summary>
/// Testes do caso de uso <see cref="Reindexar"/>: carga em lotes, troca do alias, descarte em falha e uma reindexação por vez.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReindexarTests
{
    /// <summary>
    /// Fonte de campanhas falsa.
    /// </summary>
    private readonly IFonteDeCampanhas _fonte = Substitute.For<IFonteDeCampanhas>();

    /// <summary>
    /// Gerenciador de índice falso.
    /// </summary>
    private readonly IGerenciadorDeIndice _indice = Substitute.For<IGerenciadorDeIndice>();

    /// <summary>
    /// Caso de uso sob teste.
    /// </summary>
    private readonly Reindexar _reindexar;

    /// <summary>
    /// Monta o caso de uso com índice novo chamado <c>campanhas-v2</c>.
    /// </summary>
    public ReindexarTests()
    {
        _indice.CriarIndiceNovoAsync(Arg.Any<CancellationToken>()).Returns("campanhas-v2");
        _reindexar = new Reindexar(_fonte, _indice, NullLogger<Reindexar>.Instance);
    }

    /// <summary>
    /// Dadas duas páginas de campanhas, quando a reindexação roda, então cada lote é gravado no índice novo e o alias só é trocado no fim.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_DoisLotes_GravaTodosEPublicaNoFim()
    {
        var lote1 = Lote(2);
        var lote2 = Lote(1);
        _fonte.LerEmLotesAsync(Reindexar.TamanhoDoLote, Arg.Any<CancellationToken>()).Returns(Lotes(lote1, lote2));

        var resultado = _reindexar.Iniciar();
        await _reindexar.Execucao!;

        resultado.EhSucesso.ShouldBeTrue();
        Received.InOrder(() =>
        {
            _indice.CriarIndiceNovoAsync(Arg.Any<CancellationToken>());
            _indice.GravarLoteAsync("campanhas-v2", lote1, Arg.Any<CancellationToken>());
            _indice.GravarLoteAsync("campanhas-v2", lote2, Arg.Any<CancellationToken>());
            _indice.PublicarIndiceAsync("campanhas-v2", Arg.Any<CancellationToken>());
        });
        await _indice.DidNotReceiveWithAnyArgs().DescartarIndiceAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Dado um banco sem campanhas, quando a reindexação roda, então o índice vazio é publicado.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_SemCampanhas_PublicaIndiceVazio()
    {
        _fonte.LerEmLotesAsync(Reindexar.TamanhoDoLote, Arg.Any<CancellationToken>()).Returns(Lotes());

        _reindexar.Iniciar();
        await _reindexar.Execucao!;

        await _indice.Received(1).PublicarIndiceAsync("campanhas-v2", Arg.Any<CancellationToken>());
        await _indice.DidNotReceiveWithAnyArgs().GravarLoteAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Dada uma falha ao gravar um lote, quando a reindexação roda, então o índice novo é descartado, o alias não muda e a tarefa não falha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_FalhaAoGravar_DescartaIndiceNovoSemPublicar()
    {
        _fonte.LerEmLotesAsync(Reindexar.TamanhoDoLote, Arg.Any<CancellationToken>()).Returns(Lotes(Lote(1)));
        _indice.GravarLoteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<CampanhaIndexada>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new BuscaIndisponivelException());

        _reindexar.Iniciar();
        await _reindexar.Execucao!;

        await _indice.Received(1).DescartarIndiceAsync("campanhas-v2", Arg.Any<CancellationToken>());
        await _indice.DidNotReceiveWithAnyArgs().PublicarIndiceAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Dada uma falha ao criar o índice novo, quando a reindexação roda, então não há o que descartar e a tarefa não falha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_FalhaAoCriarIndice_NaoDescartaNada()
    {
        _indice.CriarIndiceNovoAsync(Arg.Any<CancellationToken>()).Returns<string>(_ => throw new BuscaIndisponivelException());

        _reindexar.Iniciar();
        await _reindexar.Execucao!;

        await _indice.DidNotReceiveWithAnyArgs().DescartarIndiceAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Dada uma falha também ao descartar o índice novo, quando a reindexação roda, então a tarefa termina sem falhar e a trava é liberada.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_FalhaAoDescartar_TerminaSemFalharELiberaATrava()
    {
        _fonte.LerEmLotesAsync(Reindexar.TamanhoDoLote, Arg.Any<CancellationToken>()).Returns(Lotes(Lote(1)));
        _indice.PublicarIndiceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => throw new BuscaIndisponivelException());
        _indice.DescartarIndiceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => throw new BuscaIndisponivelException());

        _reindexar.Iniciar();
        await _reindexar.Execucao!;

        _reindexar.Iniciar().EhSucesso.ShouldBeTrue();
        await _reindexar.Execucao!;
    }

    /// <summary>
    /// Dada uma reindexação em andamento, quando outra é pedida, então recebe <c>busca.reindexacao_em_andamento</c>; ao terminar, uma nova é aceita.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Iniciar_ReindexacaoEmAndamento_RecusaASegundaEAceitaDepoisDeTerminar()
    {
        var liberar = new TaskCompletionSource();
        _indice.CriarIndiceNovoAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await liberar.Task;
            return "campanhas-v2";
        });
        _fonte.LerEmLotesAsync(Reindexar.TamanhoDoLote, Arg.Any<CancellationToken>()).Returns(_ => Lotes());

        var primeira = _reindexar.Iniciar();
        var segunda = _reindexar.Iniciar();
        liberar.SetResult();
        await _reindexar.Execucao!;
        var terceira = _reindexar.Iniciar();
        await _reindexar.Execucao!;

        primeira.EhSucesso.ShouldBeTrue();
        segunda.EhFalha.ShouldBeTrue();
        segunda.Erro.Codigo.ShouldBe(ErrosDeBusca.CodigoReindexacaoEmAndamento);
        terceira.EhSucesso.ShouldBeTrue();
    }

    /// <summary>
    /// Dado o caso de uso recém-criado, quando se consulta a execução, então é nula (nada foi iniciado).
    /// </summary>
    [Fact]
    public void Execucao_SemInicio_ENula()
    {
        _reindexar.Execucao.ShouldBeNull();
    }

    /// <summary>
    /// Cria um lote com a quantidade de campanhas pedida.
    /// </summary>
    /// <param name="quantidade">Quantidade de campanhas.</param>
    /// <returns>O lote.</returns>
    private static IReadOnlyList<CampanhaIndexada> Lote(int quantidade)
    {
        return [.. Enumerable.Range(1, quantidade).Select(n => new CampanhaIndexada(Guid.NewGuid(), $"Campanha {n}", "Descrição", "Ativa", 100m, new DateOnly(2026, 12, 24), n))];
    }

    /// <summary>
    /// Entrega os lotes como um fluxo assíncrono.
    /// </summary>
    /// <param name="lotes">Lotes a entregar, na ordem.</param>
    /// <returns>O fluxo de lotes.</returns>
    private static async IAsyncEnumerable<IReadOnlyList<CampanhaIndexada>> Lotes(params IReadOnlyList<CampanhaIndexada>[] lotes)
    {
        foreach (var lote in lotes)
        {
            await Task.Yield();
            yield return lote;
        }
    }
}
