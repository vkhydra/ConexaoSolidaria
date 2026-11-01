using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using MassTransit;

namespace Arrecadacao.Worker.Consumidores;

/// <summary>
/// Configura a fila <c>doacao-recebida</c>: 16 mensagens por vez, reentrega atrasada e retentativa imediata (sem repetir
/// <see cref="DoacaoNaoProcessavelException"/>) e Outbox do EF Core, que grava o <c>DoacaoProcessadaEvent</c> na mesma
/// transação do consumo e deduplica a mensagem pelo Inbox.
/// </summary>
public sealed class DoacaoRecebidaConsumerDefinition : ConsumerDefinition<DoacaoRecebidaConsumer>
{
    /// <summary>
    /// Nome da fila do consumidor.
    /// </summary>
    public const string NomeDaFila = "doacao-recebida";

    /// <summary>
    /// Quantidade de mensagens buscadas e processadas ao mesmo tempo por réplica.
    /// </summary>
    public const int Prefetch = 16;

    /// <summary>
    /// Política de retentativa aplicada à fila.
    /// </summary>
    private readonly PoliticaDeRetentativa _politica;

    /// <summary>
    /// Inicializa a definição com a política de retentativa.
    /// </summary>
    /// <param name="politica">Política de retentativa; os testes passam intervalos curtos.</param>
    public DoacaoRecebidaConsumerDefinition(PoliticaDeRetentativa politica)
    {
        _politica = politica;
        Endpoint(endpoint =>
        {
            endpoint.Name = NomeDaFila;
            endpoint.PrefetchCount = Prefetch;
            endpoint.ConcurrentMessageLimit = Prefetch;
        });
    }

    /// <inheritdoc/>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DoacaoRecebidaConsumer> consumerConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        endpointConfigurator.UseDelayedRedelivery(redelivery =>
        {
            redelivery.Intervals([.. _politica.Intervalos]);
            redelivery.Ignore<DoacaoNaoProcessavelException>();
        });
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Immediate(_politica.Imediatas);
            retry.Ignore<DoacaoNaoProcessavelException>();
        });
        endpointConfigurator.UseEntityFrameworkOutbox<CampanhasDbContext>(context);
    }
}
