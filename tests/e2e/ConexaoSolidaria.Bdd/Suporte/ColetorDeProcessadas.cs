using System.Collections.Concurrent;
using System.Text;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Guarda o corpo bruto (envelope do MassTransit) de cada <see cref="DoacaoProcessadaEvent"/> entregue, por doação.
/// </summary>
public sealed class ColetaDeProcessadas
{
    /// <summary>
    /// Obtém os envelopes recebidos, por identificador da doação.
    /// </summary>
    public ConcurrentDictionary<Guid, string> Envelopes { get; } = new();
}

/// <summary>
/// Consumidor de teste que assina o <see cref="DoacaoProcessadaEvent"/>, como a fila <c>notificacoes-recibo</c> faz, e guarda o
/// envelope para os cenários passarem à função de recibo.
/// </summary>
/// <param name="coleta">Onde os envelopes são guardados.</param>
public sealed class ColetorDeProcessadas(ColetaDeProcessadas coleta) : IConsumer<DoacaoProcessadaEvent>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<DoacaoProcessadaEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        coleta.Envelopes[context.Message.DoacaoId] = Encoding.UTF8.GetString(context.ReceiveContext.GetBody());
        return Task.CompletedTask;
    }
}
