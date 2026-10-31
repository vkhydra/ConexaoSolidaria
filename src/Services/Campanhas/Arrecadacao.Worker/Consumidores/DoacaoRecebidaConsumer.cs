using Campanhas.Application.Arrecadacao;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;

namespace Arrecadacao.Worker.Consumidores;

/// <summary>
/// Consome o <see cref="DoacaoRecebidaEvent"/> da fila <c>doacao-recebida</c> e delega ao caso de uso. Falhas transitórias
/// (banco ou MongoDB fora do ar) sobem como exceção e entram na política de retentativa; falhas de negócio viram
/// <see cref="DoacaoNaoProcessavelException"/>, que não é repetida.
/// </summary>
/// <param name="processar">Caso de uso que soma a doação ao total.</param>
public sealed class DoacaoRecebidaConsumer(ProcessarDoacao processar) : IConsumer<DoacaoRecebidaEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DoacaoRecebidaEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resultado = await processar.ExecutarAsync(context.Message, context.CancellationToken);
        if (resultado.EhFalha)
        {
            throw new DoacaoNaoProcessavelException($"A doação {context.Message.DoacaoId} não pode ser processada: {resultado.Erro.Codigo}.");
        }
    }
}
