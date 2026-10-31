using ConexaoSolidaria.Contracts.V1;
using MassTransit;

namespace Arrecadacao.Worker.IntegrationTests.Suporte;

/// <summary>
/// Consumidor de teste que assina o <see cref="DoacaoProcessadaEvent"/>, como a Notificacoes.Function fará, para que o
/// Test Harness registre o que o Outbox do Worker entregou (a entrega do Outbox usa envio direto à exchange do evento, que o
/// registro de publicações do harness não mostra).
/// </summary>
public sealed class EspiaoDeProcessadas : IConsumer<DoacaoProcessadaEvent>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<DoacaoProcessadaEvent> context)
    {
        return Task.CompletedTask;
    }
}
