using Notificacoes.Function.Recibos;
using StackExchange.Redis;

namespace Notificacoes.Function.UnitTests;

/// <summary>
/// Testes do <see cref="DeduplicadorRecibosRedis"/> com o Redis falso: formato da chave, validade e resultado da reserva.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DeduplicadorRecibosRedisTests
{
    /// <summary>
    /// Doação usada nos cenários.
    /// </summary>
    private static readonly Guid _doacao = Guid.Parse("0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f");

    /// <summary>
    /// Banco falso.
    /// </summary>
    private readonly IDatabase _banco = Substitute.For<IDatabase>();

    /// <summary>
    /// Conexão falsa que entrega o banco falso.
    /// </summary>
    private readonly IConnectionMultiplexer _conexao = Substitute.For<IConnectionMultiplexer>();

    /// <summary>
    /// Liga a conexão falsa ao banco falso.
    /// </summary>
    public DeduplicadorRecibosRedisTests()
    {
        _conexao.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_banco);
    }

    /// <summary>
    /// Dada uma doação nova, quando registrada, então o <c>SET NX EX</c> usa a chave versionada e a validade de 7 dias.
    /// </summary>
    /// <param name="reservou">Resultado do Redis: verdadeiro se a chave não existia.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TentarRegistrar_UsaSetNxComValidade(bool reservou)
    {
        _banco.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>()).Returns(reservou);

        var resultado = await new DeduplicadorRecibosRedis(_conexao).TentarRegistrarAsync(_doacao, TestContext.Current.CancellationToken);

        resultado.ShouldBe(reservou);
        await _banco.Received(1).StringSetAsync("conexao:recibos:v1:0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f", "1", TimeSpan.FromDays(7), When.NotExists);
    }

    /// <summary>
    /// Dado um cancelamento já pedido, quando o registro é tentado, então não chega ao Redis.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task TentarRegistrar_Cancelado_NaoConsultaORedis()
    {
        using var origem = new CancellationTokenSource();
        await origem.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => new DeduplicadorRecibosRedis(_conexao).TentarRegistrarAsync(_doacao, origem.Token));

        _conexao.DidNotReceiveWithAnyArgs().GetDatabase();
    }
}
