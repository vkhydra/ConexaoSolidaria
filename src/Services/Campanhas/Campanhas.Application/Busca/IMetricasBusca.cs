namespace Campanhas.Application.Busca;

/// <summary>
/// Resultado de uma busca, usado como rótulo <c>resultado</c> da métrica; poucos valores para manter a cardinalidade baixa.
/// </summary>
public enum ResultadoDaBusca
{
    /// <summary>
    /// A busca achou ao menos uma campanha.
    /// </summary>
    Ok,

    /// <summary>
    /// A busca funcionou, mas nenhuma campanha casou.
    /// </summary>
    Vazio,

    /// <summary>
    /// O mecanismo de busca estava fora do ar.
    /// </summary>
    Indisponivel,
}

/// <summary>
/// Porta das métricas da busca: <c>buscas_total{resultado}</c> e <c>campanhas_indexadas_total</c>.
/// </summary>
public interface IMetricasBusca
{
    /// <summary>
    /// Conta uma busca válida, pelo resultado. Pedidos recusados na validação não entram na conta.
    /// </summary>
    /// <param name="resultado">Resultado da busca.</param>
    void RegistrarBusca(ResultadoDaBusca resultado);

    /// <summary>
    /// Conta uma campanha gravada no índice. Eventos ignorados por versão antiga não entram na conta.
    /// </summary>
    void RegistrarIndexada();
}
