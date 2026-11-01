using Campanhas.Infrastructure.Mensageria;
using MassTransit;

namespace Campanhas.Api.Consumidores;

/// <summary>
/// Configura a fila <c>indexar-campanha</c>: retentativa imediata e reentrega atrasada quando o Elasticsearch está fora do ar.
/// Recusas do Elasticsearch (<see cref="InvalidOperationException"/>, erro 4xx) não melhoram ao repetir e vão direto para
/// <c>indexar-campanha_error</c>. A busca fica desatualizada nesse intervalo, mas a reindexação a corrige.
/// </summary>
public sealed class IndexarCampanhaConsumerDefinition : ConsumerDefinition<IndexarCampanhaConsumer>
{
    /// <summary>
    /// Nome da fila do consumidor.
    /// </summary>
    public const string NomeDaFila = "indexar-campanha";

    /// <summary>
    /// Política de retentativa aplicada à fila.
    /// </summary>
    private readonly PoliticaDeRetentativa _politica;

    /// <summary>
    /// Inicializa a definição com a política de retentativa.
    /// </summary>
    /// <param name="politica">Política de retentativa; os testes passam intervalos curtos.</param>
    public IndexarCampanhaConsumerDefinition(PoliticaDeRetentativa politica)
    {
        _politica = politica;
        Endpoint(endpoint => endpoint.Name = NomeDaFila);
    }

    /// <inheritdoc/>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<IndexarCampanhaConsumer> consumerConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);

        endpointConfigurator.UseDelayedRedelivery(redelivery =>
        {
            redelivery.Intervals([.. _politica.Intervalos]);
            redelivery.Ignore<InvalidOperationException>();
        });
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Immediate(_politica.Imediatas);
            retry.Ignore<InvalidOperationException>();
        });
    }
}
