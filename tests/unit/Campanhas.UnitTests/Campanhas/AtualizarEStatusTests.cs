using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Application.Transparencia;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.UnitTests.Suporte;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes dos casos de uso de edição e de mudança de status: versão obrigatória, conflito, não encontrada e transições.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AtualizarEStatusTests
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
    /// Cache do painel falso.
    /// </summary>
    private readonly ICachePainel _cachePainel = Substitute.For<ICachePainel>();

    /// <summary>
    /// Logger da edição.
    /// </summary>
    private readonly LoggerCaptura<AtualizarCampanha> _logAtualizar = new();

    /// <summary>
    /// Logger da mudança de status.
    /// </summary>
    private readonly LoggerCaptura<AlterarStatusCampanha> _logStatus = new();

    /// <summary>
    /// Campanha existente usada nos cenários.
    /// </summary>
    private readonly Campanha _campanha;

    /// <summary>
    /// Prepara o relógio e uma campanha ativa na versão 1.
    /// </summary>
    public AtualizarEStatusTests()
    {
        _relogio.Hoje().Returns(_hoje);
        _relogio.Agora().Returns(new DateTimeOffset(2026, 11, 10, 11, 0, 0, TimeSpan.FromHours(-3)));
        _campanha = Campanha.Criar(
            "Natal Solidário",
            "Cestas",
            Periodo.Criar(_hoje, new DateOnly(2026, 12, 24)).Valor,
            Dinheiro.Criar(10000m).Valor,
            _hoje,
            _relogio.Agora()).Valor;
        typeof(Campanha).GetProperty(nameof(Campanha.Versao))!.SetValue(_campanha, 1u);
        _campanhas.ObterPorIdAsync(_campanha.Id, Arg.Any<CancellationToken>()).Returns(_campanha);
    }

    /// <summary>
    /// Dada a versão certa, quando a meta é alterada, então grava e devolve a campanha atualizada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_VersaoCerta_AlteraEGrava()
    {
        var resultado = await CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, Pedido(meta: 12000m), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.MetaFinanceira.ShouldBe(12000m);
        await _campanhas.Received(1).SalvarAsync(_campanha, Arg.Any<CancellationToken>());
        _logAtualizar.Mensagens.ShouldHaveSingleItem().ShouldBe($"Campanha atualizada: {_campanha.Id}.");
        await _cachePainel.Received(1).RemoverAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um pedido sem versão, quando a campanha é editada, então devolve campanha.versao_obrigatoria e nada é lido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_SemVersao_DevolveVersaoObrigatoria()
    {
        var resultado = await CriarAtualizar().ExecutarAsync(_campanha.Id, null, Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoVersaoObrigatoria);
        await _campanhas.DidNotReceive().ObterPorIdAsync(Arg.Any<CampanhaId>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma versão desatualizada, quando a campanha é editada, então devolve campanha.conflito_de_versao e nada é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_VersaoDesatualizada_DevolveConflito()
    {
        var resultado = await CriarAtualizar().ExecutarAsync(_campanha.Id, 0u, Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoConflitoDeVersao);
        await _campanhas.DidNotReceive().SalvarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
        await _cachePainel.DidNotReceive().RemoverAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma edição simultânea que o banco detecta na gravação, quando a campanha é salva, então devolve campanha.conflito_de_versao.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_ConflitoNaGravacao_DevolveConflito()
    {
        _campanhas.SalvarAsync(_campanha, Arg.Any<CancellationToken>()).Returns(Task.FromException(new ConflitoDeVersaoException()));

        var resultado = await CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoConflitoDeVersao);
        _logAtualizar.Mensagens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando é editada, então devolve campanha.nao_encontrada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_CampanhaInexistente_DevolveNaoEncontrada()
    {
        var resultado = await CriarAtualizar().ExecutarAsync(CampanhaId.Novo(), 1u, Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
    }

    /// <summary>
    /// Dado um pedido inválido ou que viola o domínio, quando a campanha é editada, então devolve o erro certo e nada é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Atualizar_PedidoInvalido_DevolveErros()
    {
        var invalido = await CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, new CampanhaRequest(" x", null, null, null, null), CancellationToken.None);
        var meta = await CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, Pedido(meta: -5m), CancellationToken.None);
        _campanha.Concluir(_relogio.Agora());
        var naoEditavel = await CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, Pedido(), CancellationToken.None);

        invalido.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        meta.Erro.Codigo.ShouldBe(Campanha.CodigoMetaInvalida);
        naoEditavel.Erro.Codigo.ShouldBe(Campanha.CodigoNaoEditavel);
        await _campanhas.DidNotReceive().SalvarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um pedido nulo, quando é enviado a qualquer caso de uso de escrita, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task CasosDeUso_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => CriarAtualizar().ExecutarAsync(_campanha.Id, 1u, null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => CriarStatus().ExecutarAsync(_campanha.Id, null!, CancellationToken.None));
    }

    /// <summary>
    /// Dado um status de destino válido, quando a campanha ativa muda de status, então grava.
    /// </summary>
    /// <param name="destino">Status de destino.</param>
    /// <param name="esperado">Status que a campanha deve ter depois.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("Concluida", StatusCampanha.Concluida)]
    [InlineData("Cancelada", StatusCampanha.Cancelada)]
    public async Task Status_CampanhaAtiva_MudaEGrava(string destino, StatusCampanha esperado)
    {
        var resultado = await CriarStatus().ExecutarAsync(_campanha.Id, new AlterarStatusRequest(destino), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        _campanha.Status.ShouldBe(esperado);
        await _campanhas.Received(1).SalvarAsync(_campanha, Arg.Any<CancellationToken>());
        _logStatus.Mensagens.ShouldHaveSingleItem().ShouldBe($"Campanha {_campanha.Id} passou para {esperado}.");
        await _cachePainel.Received(1).RemoverAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma campanha concluída, quando se tenta cancelar ou voltar a Ativa, então devolve campanha.transicao_invalida.
    /// </summary>
    /// <param name="destino">Status de destino.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("Cancelada")]
    [InlineData("Concluida")]
    [InlineData("Ativa")]
    public async Task Status_CampanhaConcluida_DevolveTransicaoInvalida(string destino)
    {
        _campanha.Concluir(_relogio.Agora());

        var resultado = await CriarStatus().ExecutarAsync(_campanha.Id, new AlterarStatusRequest(destino), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoTransicaoInvalida);
        await _campanhas.DidNotReceive().SalvarAsync(Arg.Any<Campanha>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma campanha ativa, quando o destino é Ativa, então devolve campanha.transicao_invalida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Status_AtivaParaAtiva_DevolveTransicaoInvalida()
    {
        var resultado = await CriarStatus().ExecutarAsync(_campanha.Id, new AlterarStatusRequest("Ativa"), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoTransicaoInvalida);
    }

    /// <summary>
    /// Dado um status ausente ou desconhecido, quando enviado, então devolve validacao no campo status.
    /// </summary>
    /// <param name="status">Status enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Pausada")]
    [InlineData("concluida")]
    [InlineData("42")]
    public async Task Status_Desconhecido_DevolveValidacao(string? status)
    {
        var resultado = await CriarStatus().ExecutarAsync(_campanha.Id, new AlterarStatusRequest(status), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos!.Keys.ShouldBe(["status"]);
    }

    /// <summary>
    /// Dada uma campanha inexistente ou uma gravação conflitante, quando o status muda, então devolve nao_encontrada ou conflito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Status_InexistenteOuConflito_DevolveErro()
    {
        var inexistente = await CriarStatus().ExecutarAsync(CampanhaId.Novo(), new AlterarStatusRequest("Concluida"), CancellationToken.None);
        _campanhas.SalvarAsync(_campanha, Arg.Any<CancellationToken>()).Returns(Task.FromException(new ConflitoDeVersaoException()));
        var conflito = await CriarStatus().ExecutarAsync(_campanha.Id, new AlterarStatusRequest("Cancelada"), CancellationToken.None);

        inexistente.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
        conflito.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoConflitoDeVersao);
    }

    /// <summary>
    /// Monta o invalidador do painel sobre o cache falso.
    /// </summary>
    /// <returns>O invalidador.</returns>
    private InvalidadorDoPainel Invalidador()
    {
        return new InvalidadorDoPainel(_cachePainel, new LoggerCaptura<InvalidadorDoPainel>());
    }

    /// <summary>
    /// Monta o caso de uso de edição.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private AtualizarCampanha CriarAtualizar()
    {
        return new AtualizarCampanha(_campanhas, new CampanhaRequestValidator(), _relogio, Invalidador(), _logAtualizar);
    }

    /// <summary>
    /// Monta o caso de uso de mudança de status.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private AlterarStatusCampanha CriarStatus()
    {
        return new AlterarStatusCampanha(_campanhas, new AlterarStatusRequestValidator(), _relogio, Invalidador(), _logStatus);
    }

    /// <summary>
    /// Monta um pedido de edição válido, trocando só o que o teste precisa.
    /// </summary>
    /// <param name="meta">Meta do pedido.</param>
    /// <returns>O pedido.</returns>
    private static CampanhaRequest Pedido(decimal meta = 10000m)
    {
        return new CampanhaRequest("Natal Solidário", "Cestas", _hoje, new DateOnly(2026, 12, 24), meta);
    }
}
