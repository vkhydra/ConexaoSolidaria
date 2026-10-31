namespace Campanhas.Application.Abstracoes;

/// <summary>
/// Relógio da aplicação no fuso de São Paulo (<c>America/Sao_Paulo</c>), a única fonte de "hoje" e "agora" dos casos de uso.
/// </summary>
public interface IRelogio
{
    /// <summary>
    /// Obtém a data de hoje em São Paulo, que pode diferir da data em UTC no fim da noite.
    /// </summary>
    /// <returns>A data de hoje em São Paulo.</returns>
    DateOnly Hoje();

    /// <summary>
    /// Obtém o instante atual.
    /// </summary>
    /// <returns>O instante atual, com o deslocamento de São Paulo.</returns>
    DateTimeOffset Agora();
}
