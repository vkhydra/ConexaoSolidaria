using Campanhas.Application.Busca;
using ConexaoSolidaria.Contracts.V1;
using MassTransit;

namespace Campanhas.Api.Consumidores;

/// <summary>
/// Consome o <see cref="CampanhaAlteradaEvent"/> da fila <c>indexar-campanha</c> e grava a campanha no índice de busca. É
/// idempotente: a versão do evento é a versão externa do documento, então repetições e eventos fora de ordem não alteram um
/// documento mais novo. Falhas do Elasticsearch sobem como exceção e entram na política de retentativa.
/// </summary>
/// <param name="indice">Porta de escrita no índice de busca.</param>
/// <param name="metricas">Métricas da busca.</param>
public sealed class IndexarCampanhaConsumer(IIndiceCampanhas indice, IMetricasBusca metricas) : IConsumer<CampanhaAlteradaEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CampanhaAlteradaEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resultado = await indice.GravarAsync(CampanhaIndexada.De(context.Message), context.CancellationToken);
        if (resultado == ResultadoIndexacao.Indexada)
        {
            metricas.RegistrarIndexada();
        }
    }
}
