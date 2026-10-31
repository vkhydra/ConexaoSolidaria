using Campanhas.Application.Transparencia;
using Campanhas.UnitTests.Suporte;

namespace Campanhas.UnitTests.Transparencia;

/// <summary>
/// Testes do invalidador do cache do painel: remove o cache e tolera a falha do Redis.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InvalidadorDoPainelTests
{
    /// <summary>
    /// Cache do painel falso.
    /// </summary>
    private readonly ICachePainel _cache = Substitute.For<ICachePainel>();

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<InvalidadorDoPainel> _log = new();

    /// <summary>
    /// Dado um cache saudável, quando o painel é invalidado, então o cache é removido sem aviso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Invalidar_CacheSaudavel_Remove()
    {
        await new InvalidadorDoPainel(_cache, _log).InvalidarAsync(CancellationToken.None);

        await _cache.Received(1).RemoverAsync(Arg.Any<CancellationToken>());
        _log.Mensagens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um Redis fora do ar, quando o painel é invalidado, então a falha é registrada sem dados sensíveis e não sobe.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Invalidar_CacheFalha_RegistraAvisoENaoPropaga()
    {
        _cache.RemoverAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("senha=segredo")));

        await new InvalidadorDoPainel(_cache, _log).InvalidarAsync(CancellationToken.None);

        _log.Mensagens.ShouldHaveSingleItem().ShouldBe("Não foi possível invalidar o painel no cache (InvalidOperationException); ele expira sozinho em instantes.");
    }

    /// <summary>
    /// Dado um cancelamento durante a remoção, quando o painel é invalidado, então o cancelamento é propagado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Invalidar_Cancelado_PropagaCancelamento()
    {
        _cache.RemoverAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new OperationCanceledException()));

        await Should.ThrowAsync<OperationCanceledException>(() => new InvalidadorDoPainel(_cache, _log).InvalidarAsync(CancellationToken.None));
    }
}
