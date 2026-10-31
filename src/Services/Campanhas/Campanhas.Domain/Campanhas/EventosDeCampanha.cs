namespace Campanhas.Domain.Campanhas;

/// <summary>
/// Fato ocorrido no domínio. A camada de aplicação o converte em evento de integração pelo Outbox.
/// </summary>
public interface IEventoDeDominio
{
    /// <summary>
    /// Obtém a campanha à qual o fato se refere.
    /// </summary>
    CampanhaId CampanhaId { get; }

    /// <summary>
    /// Obtém o instante em que o fato ocorreu.
    /// </summary>
    DateTimeOffset OcorridoEm { get; }
}

/// <summary>
/// Uma campanha foi criada.
/// </summary>
/// <param name="CampanhaId">Campanha criada.</param>
/// <param name="OcorridoEm">Instante da criação.</param>
public sealed record CampanhaCriada(CampanhaId CampanhaId, DateTimeOffset OcorridoEm) : IEventoDeDominio;

/// <summary>
/// Os dados de uma campanha foram alterados.
/// </summary>
/// <param name="CampanhaId">Campanha alterada.</param>
/// <param name="OcorridoEm">Instante da alteração.</param>
public sealed record CampanhaAtualizada(CampanhaId CampanhaId, DateTimeOffset OcorridoEm) : IEventoDeDominio;

/// <summary>
/// Uma campanha foi concluída.
/// </summary>
/// <param name="CampanhaId">Campanha concluída.</param>
/// <param name="OcorridoEm">Instante da conclusão.</param>
public sealed record CampanhaConcluida(CampanhaId CampanhaId, DateTimeOffset OcorridoEm) : IEventoDeDominio;

/// <summary>
/// Uma campanha foi cancelada.
/// </summary>
/// <param name="CampanhaId">Campanha cancelada.</param>
/// <param name="OcorridoEm">Instante do cancelamento.</param>
public sealed record CampanhaCancelada(CampanhaId CampanhaId, DateTimeOffset OcorridoEm) : IEventoDeDominio;
