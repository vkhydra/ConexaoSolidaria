using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;

namespace Campanhas.Application.Abstracoes;

/// <summary>
/// Porta de persistência das campanhas. A implementação (EF Core, PostgreSQL e Outbox do MassTransit) fica na infraestrutura
/// e grava os dados e o evento <c>CampanhaAlteradaEvent</c> na mesma transação.
/// </summary>
public interface ICampanhaRepositorio
{
    /// <summary>
    /// Busca uma campanha pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha ou nulo se não existir.</returns>
    Task<Campanha?> ObterPorIdAsync(CampanhaId id, CancellationToken cancelamento);

    /// <summary>
    /// Lista campanhas com filtro opcional de status, ordenadas pela data fim, e paginadas.
    /// </summary>
    /// <param name="status">Status a filtrar; nulo traz todos.</param>
    /// <param name="pagina">Número da página, a partir de 1.</param>
    /// <param name="tamanhoPagina">Itens por página, de 1 a 100.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página pedida e o total de campanhas que passam no filtro.</returns>
    Task<Pagina<Campanha>> ListarAsync(StatusCampanha? status, int pagina, int tamanhoPagina, CancellationToken cancelamento);

    /// <summary>
    /// Grava uma campanha nova e publica o evento de alteração, na mesma transação.
    /// </summary>
    /// <param name="campanha">Campanha criada.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando a campanha foi gravada.</returns>
    Task AdicionarAsync(Campanha campanha, CancellationToken cancelamento);

    /// <summary>
    /// Grava as alterações de uma campanha carregada por este repositório e publica o evento de alteração, na mesma transação.
    /// </summary>
    /// <param name="campanha">Campanha alterada.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando as alterações foram gravadas.</returns>
    /// <exception cref="ConflitoDeVersaoException">Outra gravação alterou a campanha depois que ela foi carregada.</exception>
    Task SalvarAsync(Campanha campanha, CancellationToken cancelamento);
}
