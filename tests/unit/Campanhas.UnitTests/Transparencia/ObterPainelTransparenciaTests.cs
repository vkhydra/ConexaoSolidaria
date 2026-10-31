using Campanhas.Application.Abstracoes;
using Campanhas.Application.Transparencia;
using Campanhas.UnitTests.Suporte;

namespace Campanhas.UnitTests.Transparencia;

/// <summary>
/// Testes do caso de uso do painel de transparência: percentual, lista vazia, ordem e falha do cache.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ObterPainelTransparenciaTests
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Consulta ao banco falsa.
    /// </summary>
    private readonly ICampanhaLeitura _leitura = Substitute.For<ICampanhaLeitura>();

    /// <summary>
    /// Cache falso.
    /// </summary>
    private readonly ICachePainel _cache = Substitute.For<ICachePainel>();

    /// <summary>
    /// Relógio falso fixado em hoje.
    /// </summary>
    private readonly IRelogio _relogio = Substitute.For<IRelogio>();

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<ObterPainelTransparencia> _log = new();

    /// <summary>
    /// Prepara o relógio e faz o cache falso chamar a fábrica (cache vazio).
    /// </summary>
    public ObterPainelTransparenciaTests()
    {
        _relogio.Hoje().Returns(_hoje);
        _cache.ObterOuCriarAsync(Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns(chamada => chamada.Arg<Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>>>()(chamada.Arg<CancellationToken>()));
    }

    /// <summary>
    /// Dada uma campanha com 150.00 de 10000.00, quando o painel é consultado, então o percentual atingido é 1.50.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_CampanhaComDoacao_CalculaPercentual()
    {
        var id = Guid.NewGuid();
        _leitura.ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>()).Returns([new CampanhaDoPainel(id, "A", 10000m, 150m, new DateOnly(2026, 12, 24))]);

        var painel = await Criar().ExecutarAsync(CancellationToken.None);

        var item = painel.ShouldHaveSingleItem();
        item.ShouldBe(new PainelItemResponse(id, "A", 10000m, 150m, 1.50m, new DateOnly(2026, 12, 24)));
    }

    /// <summary>
    /// Dado um banco sem campanhas ativas, quando o painel é consultado, então a lista é vazia.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_SemCampanhas_DevolveListaVazia()
    {
        _leitura.ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>()).Returns([]);

        (await Criar().ExecutarAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    /// <summary>
    /// Dadas linhas na ordem do banco, quando o painel é montado, então a ordem é mantida e meta zero não divide por zero.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_VariasLinhas_MantemAOrdemEProtegeMetaZero()
    {
        _leitura.ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>()).Returns(
        [
            new CampanhaDoPainel(Guid.NewGuid(), "Primeira", 1000m, 1000m, new DateOnly(2026, 11, 20)),
            new CampanhaDoPainel(Guid.NewGuid(), "Segunda", 0m, 5m, new DateOnly(2026, 12, 1)),
        ]);

        var painel = await Criar().ExecutarAsync(CancellationToken.None);

        painel.Select(p => p.Titulo).ShouldBe(["Primeira", "Segunda"]);
        painel[0].PercentualAtingido.ShouldBe(100m);
        painel[1].PercentualAtingido.ShouldBe(0m);
    }

    /// <summary>
    /// Dado um cache que falha (Redis fora do ar), quando o painel é consultado, então lê direto do banco e registra um aviso sem dados sensíveis.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_CacheFalha_LeDiretoDoBanco()
    {
        _cache.ObterOuCriarAsync(Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<PainelItemResponse>>>(_ => throw new InvalidOperationException("Redis fora do ar, senha=segredo"));
        _leitura.ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>()).Returns([new CampanhaDoPainel(Guid.NewGuid(), "A", 100m, 10m, new DateOnly(2026, 12, 24))]);

        var painel = await Criar().ExecutarAsync(CancellationToken.None);

        painel.ShouldHaveSingleItem().PercentualAtingido.ShouldBe(10m);
        _log.Mensagens.ShouldHaveSingleItem().ShouldBe("Cache do painel indisponível (InvalidOperationException); lendo direto do banco.");
    }

    /// <summary>
    /// Dado um banco que falha, quando o painel é consultado, então a falha sobe sem tentar o banco duas vezes.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_BancoFalha_PropagaSemRepetirAConsulta()
    {
        _leitura.ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<CampanhaDoPainel>>>(_ => throw new InvalidOperationException("banco"));

        await Should.ThrowAsync<InvalidOperationException>(() => Criar().ExecutarAsync(CancellationToken.None));

        await _leitura.Received(1).ListarAtivasAsync(_hoje, Arg.Any<CancellationToken>());
        _log.Mensagens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um cancelamento durante a consulta, quando o painel é consultado, então o cancelamento é propagado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Executar_Cancelado_PropagaCancelamento()
    {
        _cache.ObterOuCriarAsync(Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<PainelItemResponse>>>(_ => throw new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => Criar().ExecutarAsync(CancellationToken.None));
    }

    /// <summary>
    /// Monta o caso de uso com os dublês do teste.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private ObterPainelTransparencia Criar()
    {
        return new ObterPainelTransparencia(_leitura, _cache, _relogio, _log);
    }
}
