using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Campanhas;
using Campanhas.Domain.Campanhas;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.Logging.Abstractions;

namespace Campanhas.UnitTests.Arrecadacao;

/// <summary>
/// Testes do caso de uso <see cref="ProcessarDoacao"/>: soma, reentrega e falhas permanentes.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ProcessarDoacaoTests
{
    /// <summary>
    /// Instante do processamento nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 10, 6, TimeSpan.FromHours(-3));

    /// <summary>
    /// Porta de registro falsa.
    /// </summary>
    private readonly IRegistroArrecadacao _registro = Substitute.For<IRegistroArrecadacao>();

    /// <summary>
    /// Livro de doações falso.
    /// </summary>
    private readonly ILivroDoacoes _livro = Substitute.For<ILivroDoacoes>();

    /// <summary>
    /// Relógio falso fixo em <see cref="_agora"/>.
    /// </summary>
    private readonly IRelogio _relogio = Substitute.For<IRelogio>();

    /// <summary>
    /// Prepara o relógio.
    /// </summary>
    public ProcessarDoacaoTests()
    {
        _relogio.Agora().Returns(_agora);
    }

    /// <summary>
    /// Dada uma doação nova, quando processada, então o registro é pedido com o instante do relógio e o total é devolvido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_DoacaoNova_SomaEDevolveOTotal()
    {
        var doacao = NovaDoacao(150m);
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.Registrada, _agora, 1150m));

        var resultado = await Criar().ExecutarAsync(doacao, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.JaProcessada.ShouldBeFalse();
        resultado.Valor.TotalArrecadado.ShouldBe(1150m);
        resultado.Valor.ProcessadaEm.ShouldBe(_agora);
        await _livro.Received(1).RegistrarAsync(
            new LancamentoDoacao(doacao.DoacaoId, doacao.CampanhaId, doacao.DoadorId, 150m, 1150m, doacao.OcorridoEm, _agora),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma reentrega, quando processada, então é sucesso marcado como já processada, com o instante da primeira vez.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_Reentrega_DevolveJaProcessada()
    {
        var doacao = NovaDoacao(150m);
        var primeira = _agora.AddMinutes(-5);
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.JaProcessada, primeira, 1150m));

        var resultado = await Criar().ExecutarAsync(doacao, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.JaProcessada.ShouldBeTrue();
        resultado.Valor.ProcessadaEm.ShouldBe(primeira);
        await _livro.Received(1).RegistrarAsync(
            Arg.Is<LancamentoDoacao>(l => l.DoacaoId == doacao.DoacaoId && l.ProcessadaEm == primeira && l.TotalAposLancamento == 1150m),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado que o livro falha depois de o total ter sido somado, quando a doação é processada, então a exceção sobe para
    /// a retentativa, que na reentrega completa o livro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_LivroIndisponivel_PropagaAExcecao()
    {
        var doacao = NovaDoacao(150m);
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.Registrada, _agora, 150m));
        _livro.RegistrarAsync(Arg.Any<LancamentoDoacao>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));

        await Should.ThrowAsync<TimeoutException>(() => Criar().ExecutarAsync(doacao, CancellationToken.None));
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando processada, então devolve campanha.nao_encontrada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_CampanhaInexistente_DevolveNaoEncontrada()
    {
        var doacao = NovaDoacao(10m);
        _registro.RegistrarAsync(doacao, _agora, Arg.Any<CancellationToken>()).Returns(new ResultadoRegistro(SituacaoRegistro.CampanhaInexistente, null, null));

        var resultado = await Criar().ExecutarAsync(doacao, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeCampanha.CodigoNaoEncontrada);
        await _livro.DidNotReceive().RegistrarAsync(Arg.Any<LancamentoDoacao>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um valor zero ou negativo, quando processada, então devolve doacao.valor_invalido sem tocar no registro.
    /// </summary>
    /// <param name="valor">Valor da doação.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Processar_ValorNaoPositivo_DevolveValorInvalido(int valor)
    {
        var resultado = await Criar().ExecutarAsync(NovaDoacao(valor), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoDoacaoValorInvalido);
        await _registro.DidNotReceive().RegistrarAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma falha do banco, quando processada, então a exceção sobe para a camada de mensageria tentar de novo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_FalhaTransitoria_PropagaAExcecao()
    {
        _registro.RegistrarAsync(Arg.Any<DoacaoRecebidaEvent>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResultadoRegistro>(new TimeoutException()));

        await Should.ThrowAsync<TimeoutException>(() => Criar().ExecutarAsync(NovaDoacao(10m), CancellationToken.None));
    }

    /// <summary>
    /// Dada uma doação nula, quando processada, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Processar_DoacaoNula_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Criar().ExecutarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Monta o caso de uso com as dependências falsas.
    /// </summary>
    /// <returns>O caso de uso.</returns>
    private ProcessarDoacao Criar()
    {
        return new ProcessarDoacao(_registro, _livro, _relogio, NullLogger<ProcessarDoacao>.Instance);
    }

    /// <summary>
    /// Cria uma doação recebida com o valor informado.
    /// </summary>
    /// <param name="valor">Valor da doação.</param>
    /// <returns>O evento de doação recebida.</returns>
    private static DoacaoRecebidaEvent NovaDoacao(decimal valor)
    {
        return new DoacaoRecebidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), valor, _agora.AddSeconds(-1));
    }
}
