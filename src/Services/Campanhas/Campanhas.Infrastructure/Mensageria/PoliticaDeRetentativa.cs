namespace Campanhas.Infrastructure.Mensageria;

/// <summary>
/// Política de retentativa dos consumidores: tentativas imediatas e, depois, reentregas atrasadas. Esgotadas, a
/// mensagem vai para a fila <c>{fila}_error</c>.
/// </summary>
/// <param name="Imediatas">Quantidade de retentativas imediatas.</param>
/// <param name="Intervalos">Espera de cada reentrega atrasada, em ordem.</param>
public sealed record PoliticaDeRetentativa(int Imediatas, IReadOnlyList<TimeSpan> Intervalos)
{
    /// <summary>
    /// Obtém a política do produto: 3 retentativas imediatas e reentregas após 5 s, 30 s e 2 min.
    /// </summary>
    public static PoliticaDeRetentativa Padrao { get; } = new(3, [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)]);
}
