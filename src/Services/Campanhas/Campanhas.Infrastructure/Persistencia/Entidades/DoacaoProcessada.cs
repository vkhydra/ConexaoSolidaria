using Campanhas.Domain.Campanhas;

namespace Campanhas.Infrastructure.Persistencia.Entidades;

/// <summary>
/// Registro de uma doação já somada ao total da campanha. A chave primária é o identificador da doação, o que torna o
/// processamento idempotente: inserir de novo a mesma doação viola a chave e indica reentrega.
/// </summary>
public sealed class DoacaoProcessada
{
    /// <summary>
    /// Obtém ou define o identificador da doação, chave primária.
    /// </summary>
    public Guid DoacaoId { get; set; }

    /// <summary>
    /// Obtém ou define a campanha que recebeu a doação.
    /// </summary>
    public CampanhaId CampanhaId { get; set; }

    /// <summary>
    /// Obtém ou define o valor somado ao total, sempre maior que zero.
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Obtém ou define o instante em que o Worker somou a doação.
    /// </summary>
    public DateTimeOffset ProcessadaEm { get; set; }
}
