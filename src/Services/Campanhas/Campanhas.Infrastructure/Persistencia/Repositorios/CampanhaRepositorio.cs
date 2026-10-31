using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Infrastructure.Mensageria;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Repositório de campanhas sobre o <see cref="CampanhasDbContext"/>. Ao gravar, publica o
/// <c>CampanhaAlteradaEvent</c> pelo Outbox, então dados e evento entram na mesma transação.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas.</param>
/// <param name="publicador">Publicador do MassTransit, que no modo bus outbox grava na tabela <c>outbox_message</c>.</param>
public sealed class CampanhaRepositorio(CampanhasDbContext contexto, IPublishEndpoint publicador) : ICampanhaRepositorio
{
    /// <inheritdoc/>
    public Task<Campanha?> ObterPorIdAsync(CampanhaId id, CancellationToken cancelamento)
    {
        return contexto.Campanhas.FirstOrDefaultAsync(c => c.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<Pagina<Campanha>> ListarAsync(StatusCampanha? status, int pagina, int tamanhoPagina, CancellationToken cancelamento)
    {
        var consulta = contexto.Campanhas.AsNoTracking();
        if (status is not null)
        {
            consulta = consulta.Where(c => c.Status == status);
        }

        var total = await consulta.CountAsync(cancelamento);
        var itens = await consulta
            .OrderBy(c => c.Periodo.DataFim)
            .ThenBy(c => c.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancelamento);

        return new Pagina<Campanha>(itens, pagina, tamanhoPagina, total);
    }

    /// <inheritdoc/>
    public async Task AdicionarAsync(Campanha campanha, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        contexto.Campanhas.Add(campanha);
        await GravarComEventoAsync(campanha, cancelamento);
    }

    /// <inheritdoc/>
    public async Task SalvarAsync(Campanha campanha, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        try
        {
            await GravarComEventoAsync(campanha, cancelamento);
        }
        catch (DbUpdateConcurrencyException excecao)
        {
            throw new ConflitoDeVersaoException("A campanha foi alterada por outra gravação.", excecao);
        }
    }

    /// <summary>
    /// Publica o evento de alteração (se o agregado registrou algum fato) e grava tudo numa só transação.
    /// </summary>
    /// <param name="campanha">Campanha criada ou alterada.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando a campanha e o evento foram gravados.</returns>
    private async Task GravarComEventoAsync(Campanha campanha, CancellationToken cancelamento)
    {
        if (campanha.Eventos.Count > 0)
        {
            await publicador.Publish(EventosDeIntegracao.ParaAlterada(campanha), cancelamento);
        }

        await contexto.SaveChangesAsync(cancelamento);
        campanha.LimparEventos();
    }
}
