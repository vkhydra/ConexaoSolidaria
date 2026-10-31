using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.Application.Doacoes;

/// <summary>
/// Porta de publicação das doações aceitas. A implementação grava o evento no Outbox do MassTransit, junto com a chave de
/// idempotência, na mesma transação, e o serviço em segundo plano o entrega ao RabbitMQ.
/// </summary>
public interface IPublicadorDeDoacoes
{
    /// <summary>
    /// Registra no Outbox o evento de uma doação aceita e, se houver, a chave de idempotência da requisição.
    /// </summary>
    /// <param name="evento">Evento da doação recebida.</param>
    /// <param name="chave">Chave de idempotência a gravar na mesma transação; nulo quando o cliente não enviou o cabeçalho.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o evento foi gravado no Outbox.</returns>
    /// <exception cref="ChaveJaRegistradaException">Outra requisição registrou a mesma chave do mesmo doador no mesmo instante.</exception>
    Task PublicarRecebidaAsync(DoacaoRecebidaEvent evento, ChaveParaRegistrar? chave, CancellationToken cancelamento);
}
