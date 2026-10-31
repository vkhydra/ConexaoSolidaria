using Campanhas.Application.Doacoes;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Entidades;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Campanhas.Infrastructure.Mensageria;

/// <summary>
/// Publica a doação aceita pelo Outbox do MassTransit: o evento e a chave de idempotência são gravados pelo mesmo
/// <see cref="CampanhasDbContext"/>, na mesma transação, e o serviço em segundo plano entrega o evento ao RabbitMQ.
/// Se o broker estiver fora do ar, a doação aceita não se perde.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas.</param>
/// <param name="publicador">Publicador do MassTransit, que no modo bus outbox grava na tabela <c>outbox_message</c>.</param>
public sealed class PublicadorDeDoacoes(CampanhasDbContext contexto, IPublishEndpoint publicador) : IPublicadorDeDoacoes
{
    /// <inheritdoc/>
    public async Task PublicarRecebidaAsync(DoacaoRecebidaEvent evento, ChaveParaRegistrar? chave, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        if (chave is not null)
        {
            contexto.ChavesIdempotencia.Add(new ChaveIdempotencia
            {
                Chave = chave.Chave,
                DoadorId = evento.DoadorId,
                DoacaoId = evento.DoacaoId,
                HashCorpo = chave.HashCorpo,
                CriadaEm = evento.OcorridoEm,
            });
        }

        await publicador.Publish(evento, cancelamento);

        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ChaveJaRegistradaException("A Idempotency-Key já foi registrada por outra requisição.", excecao);
        }
    }
}
