namespace Campanhas.Application.Transparencia;

/// <summary>
/// Porta do cache do painel (HybridCache com Redis). O cache é uma otimização: o caso de uso funciona sem ele.
/// </summary>
public interface ICachePainel
{
    /// <summary>
    /// Devolve o painel em cache ou, se não houver, chama a fábrica, guarda o resultado e o devolve.
    /// </summary>
    /// <param name="fabrica">Consulta ao banco, chamada só em caso de ausência no cache.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O painel, do cache ou do banco.</returns>
    Task<IReadOnlyList<PainelItemResponse>> ObterOuCriarAsync(
        Func<CancellationToken, Task<IReadOnlyList<PainelItemResponse>>> fabrica,
        CancellationToken cancelamento);

    /// <summary>
    /// Remove o painel do cache, para que a próxima consulta leia o banco.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o painel foi removido.</returns>
    Task RemoverAsync(CancellationToken cancelamento);
}
