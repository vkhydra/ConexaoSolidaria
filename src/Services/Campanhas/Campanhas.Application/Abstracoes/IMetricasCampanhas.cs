namespace Campanhas.Application.Abstracoes;

/// <summary>
/// Porta das métricas de campanhas: <c>campanhas_criadas_total</c>.
/// </summary>
public interface IMetricasCampanhas
{
    /// <summary>
    /// Conta uma campanha criada com sucesso.
    /// </summary>
    void RegistrarCriacao();
}
