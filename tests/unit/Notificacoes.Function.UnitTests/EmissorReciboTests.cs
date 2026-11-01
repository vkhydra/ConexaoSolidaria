using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.Logging;
using Notificacoes.Function.Recibos;

namespace Notificacoes.Function.UnitTests;

/// <summary>
/// Testes do <see cref="EmissorRecibo"/>: conteúdo do log de recibo (sem dados pessoais) e tratamento de doação repetida.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmissorReciboTests
{
    /// <summary>
    /// Doação usada nos cenários.
    /// </summary>
    private static readonly DoacaoProcessadaEvent _doacao = new(
        Guid.Parse("0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f"),
        Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f"),
        Guid.Parse("0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70"),
        150.00m,
        1150.00m,
        new DateTimeOffset(2026, 11, 10, 17, 10, 5, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 10, 17, 10, 7, TimeSpan.Zero));

    /// <summary>
    /// Deduplicador falso.
    /// </summary>
    private readonly IDeduplicadorRecibos _deduplicador = Substitute.For<IDeduplicadorRecibos>();

    /// <summary>
    /// Logger que guarda o que recebe.
    /// </summary>
    private readonly LoggerDeTeste _log = new();

    /// <summary>
    /// Dada uma doação nova, quando o recibo é emitido, então o log <c>ReciboEmitido</c> traz só identificadores e valor.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Emitir_DoacaoNova_RegistraReciboSemDadosPessoais()
    {
        _deduplicador.TentarRegistrarAsync(_doacao.DoacaoId, Arg.Any<CancellationToken>()).Returns(true);

        var emitido = await new EmissorRecibo(_deduplicador, _log).EmitirAsync(_doacao, TestContext.Current.CancellationToken);

        emitido.ShouldBeTrue();
        var registro = _log.Registros.ShouldHaveSingleItem();
        registro.Nivel.ShouldBe(LogLevel.Information);
        registro.NomeDoEvento.ShouldBe("ReciboEmitido");
        registro.Mensagem.ShouldStartWith("ReciboEmitido para a doação 0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f da campanha 0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f no valor de ");
        registro.Mensagem.ShouldNotContain("@");
        registro.Campos.Keys.Order().ShouldBe(["CampanhaId", "DoacaoId", "DoadorId", "TraceId", "Valor"]);
        registro.Campos["DoadorId"].ShouldBe(_doacao.DoadorId);
        registro.Campos["Valor"].ShouldBe(150.00m);
    }

    /// <summary>
    /// Dada uma doação que já teve recibo, quando a fila a entrega de novo, então nenhum recibo é emitido (só um log de depuração).
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Emitir_DoacaoRepetida_NaoEmiteSegundoRecibo()
    {
        _deduplicador.TentarRegistrarAsync(_doacao.DoacaoId, Arg.Any<CancellationToken>()).Returns(false);

        var emitido = await new EmissorRecibo(_deduplicador, _log).EmitirAsync(_doacao, TestContext.Current.CancellationToken);

        emitido.ShouldBeFalse();
        var registro = _log.Registros.ShouldHaveSingleItem();
        registro.Nivel.ShouldBe(LogLevel.Debug);
        registro.NomeDoEvento.ShouldBe("ReciboDuplicadoIgnorado");
    }

    /// <summary>
    /// Dada uma doação nula, quando o recibo é pedido, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Emitir_DoacaoNula_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => new EmissorRecibo(_deduplicador, _log).EmitirAsync(null!, TestContext.Current.CancellationToken));
    }
}
