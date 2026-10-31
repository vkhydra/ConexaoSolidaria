using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.Application.Doacoes;

/// <summary>
/// Porta de publicação das doações aceitas. A implementação grava o evento no Outbox do MassTransit, na mesma transação
/// de qualquer outro dado do aceite, e o serviço em segundo plano o entrega ao RabbitMQ.
/// </summary>
public interface IPublicadorDeDoacoes
{
    /// <summary>
    /// Registra no Outbox o evento de uma doação aceita.
    /// </summary>
    /// <param name="evento">Evento da doação recebida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o evento foi gravado no Outbox.</returns>
    Task PublicarRecebidaAsync(DoacaoRecebidaEvent evento, CancellationToken cancelamento);
}
