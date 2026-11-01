using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notificacoes.Function.Recibos;
using StackExchange.Redis;

namespace Notificacoes.Function.UnitTests;

/// <summary>
/// Testes da função <see cref="EmitirRecibo"/> (envelope entregue pelo gatilho) e do registro dos serviços.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmitirReciboTests
{
    /// <summary>
    /// Envelope de uma doação processada.
    /// </summary>
    private const string _envelope = """
        { "messageType": [ "urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent" ],
          "message": { "doacaoId": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f", "campanhaId": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
                       "doadorId": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70", "valor": "150.00", "totalArrecadado": "1150.00",
                       "recebidaEm": "2026-11-10T17:10:05+00:00", "processadaEm": "2026-11-10T17:10:07+00:00" } }
        """;

    /// <summary>
    /// Deduplicador falso.
    /// </summary>
    private readonly IDeduplicadorRecibos _deduplicador = Substitute.For<IDeduplicadorRecibos>();

    /// <summary>
    /// Logger do emissor, que guarda o recibo.
    /// </summary>
    private readonly LoggerDeTeste _logDoEmissor = new();

    /// <summary>
    /// Dado o envelope de uma doação nova, quando a função roda, então o recibo é emitido uma vez.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_DoacaoNova_EmiteRecibo()
    {
        _deduplicador.TentarRegistrarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        await CriarFuncao().ExecutarAsync(_envelope, TestContext.Current.CancellationToken);

        _logDoEmissor.Registros.ShouldHaveSingleItem().NomeDoEvento.ShouldBe("ReciboEmitido");
    }

    /// <summary>
    /// Dado o mesmo envelope entregue de novo, quando a função roda, então nenhum segundo recibo é emitido.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_DoacaoRepetida_NaoEmiteSegundoRecibo()
    {
        _deduplicador.TentarRegistrarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true, false);
        var funcao = CriarFuncao();

        await funcao.ExecutarAsync(_envelope, TestContext.Current.CancellationToken);
        await funcao.ExecutarAsync(_envelope, TestContext.Current.CancellationToken);

        _logDoEmissor.Registros.Count(r => r.NomeDoEvento == "ReciboEmitido").ShouldBe(1);
    }

    /// <summary>
    /// Dado o envelope de outro tipo de mensagem, quando a função roda, então ele é ignorado sem consultar o deduplicador.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Executar_OutroTipo_IgnoraSemEmitir()
    {
        var outro = _envelope.Replace("DoacaoProcessadaEvent", "CampanhaAlteradaEvent", StringComparison.Ordinal);

        await CriarFuncao().ExecutarAsync(outro, TestContext.Current.CancellationToken);

        await _deduplicador.DidNotReceiveWithAnyArgs().TentarRegistrarAsync(default, TestContext.Current.CancellationToken);
        _logDoEmissor.Registros.ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um corpo que não é envelope, quando a função roda, então a exceção sobe para o runtime repetir e mandar à fila de erro.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Executar_EnvelopeInvalido_LancaExcecao()
    {
        await Should.ThrowAsync<EnvelopeInvalidoException>(() => CriarFuncao().ExecutarAsync("lixo", TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dado o Redis fora do ar, quando a função roda, então a falha sobe (a mensagem será repetida, não perdida).
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Executar_RedisForaDoAr_PropagaAFalha()
    {
        _deduplicador.TentarRegistrarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns<bool>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "fora"));

        await Should.ThrowAsync<RedisConnectionException>(() => CriarFuncao().ExecutarAsync(_envelope, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dada a conexão do Redis configurada, quando os serviços são registrados, então emissor e deduplicador são resolvidos
    /// (a conexão só abre no primeiro uso).
    /// </summary>
    [Fact]
    public void AddNotificacoes_ComRedis_RegistraOsServicos()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Redis"] = "localhost:1,abortConnect=false" })
            .Build();
        var servicos = new ServiceCollection().AddLogging();

        servicos.AddNotificacoes(configuracao);

        using var provedor = servicos.BuildServiceProvider();
        provedor.GetRequiredService<EmissorRecibo>().ShouldNotBeNull();
    }

    /// <summary>
    /// Dada a conexão do Redis ausente ou argumentos nulos, quando os serviços são registrados, então a função não sobe.
    /// </summary>
    [Fact]
    public void AddNotificacoes_ConfiguracaoInvalida_LancaExcecao()
    {
        var vazia = new ConfigurationBuilder().Build();

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddNotificacoes(vazia));
        Should.Throw<ArgumentNullException>(() => ExtensoesNotificacoes.AddNotificacoes(null!, vazia));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddNotificacoes(null!));
    }

    /// <summary>
    /// Dados os nomes de fila e de conexão, quando consultados, então seguem o contrato da feature.
    /// </summary>
    [Fact]
    public void Constantes_FilaEConexao_SeguemOContrato()
    {
        EmitirRecibo.Fila.ShouldBe("notificacoes-recibo");
        EmitirRecibo.ConfiguracaoDaConexao.ShouldBe("RabbitMqConnection");
    }

    /// <summary>
    /// Monta a função com o emissor real sobre o deduplicador e o logger falsos.
    /// </summary>
    /// <returns>A função.</returns>
    private EmitirRecibo CriarFuncao()
    {
        return new EmitirRecibo(new EmissorRecibo(_deduplicador, _logDoEmissor), Substitute.For<ILogger<EmitirRecibo>>());
    }
}
