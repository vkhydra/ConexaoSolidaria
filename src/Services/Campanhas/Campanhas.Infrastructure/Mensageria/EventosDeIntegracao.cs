using Campanhas.Domain.Campanhas;
using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.Infrastructure.Mensageria;

/// <summary>
/// Converte o estado do agregado nos eventos de integração publicados pelo Outbox.
/// </summary>
public static class EventosDeIntegracao
{
    /// <summary>
    /// Monta o <see cref="CampanhaAlteradaEvent"/> com o estado atual da campanha. A versão é o instante UTC da última
    /// alteração em ticks, que cresce a cada alteração e já é conhecida antes de a linha ser gravada.
    /// </summary>
    /// <param name="campanha">Campanha criada ou alterada.</param>
    /// <returns>O evento de alteração, sem dado pessoal.</returns>
    public static CampanhaAlteradaEvent ParaAlterada(Campanha campanha)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        return new CampanhaAlteradaEvent(
            campanha.Id.Valor,
            campanha.Titulo,
            campanha.Descricao,
            campanha.Status.ToString(),
            campanha.MetaFinanceira.Valor,
            campanha.Periodo.DataFim,
            campanha.AtualizadoEm.UtcTicks);
    }
}
