using Campanhas.Application.Doacoes;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;

namespace Campanhas.Infrastructure.Mensageria;

/// <summary>
/// Publica a doação aceita pelo Outbox do MassTransit: o evento é gravado em <c>outbox_message</c> pelo mesmo
/// <see cref="CampanhasDbContext"/>, e o serviço em segundo plano o entrega ao RabbitMQ. Se o broker estiver fora do ar,
/// a doação aceita não se perde.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas.</param>
/// <param name="publicador">Publicador do MassTransit, que no modo bus outbox grava na tabela <c>outbox_message</c>.</param>
public sealed class PublicadorDeDoacoes(CampanhasDbContext contexto, IPublishEndpoint publicador) : IPublicadorDeDoacoes
{
    /// <inheritdoc/>
    public async Task PublicarRecebidaAsync(DoacaoRecebidaEvent evento, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        await publicador.Publish(evento, cancelamento);
        await contexto.SaveChangesAsync(cancelamento);
    }
}
