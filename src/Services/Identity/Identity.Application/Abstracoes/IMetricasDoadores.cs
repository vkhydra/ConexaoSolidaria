namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta da métrica <c>doadores_cadastrados_total</c>.
/// </summary>
public interface IMetricasDoadores
{
    /// <summary>
    /// Conta um doador cadastrado com sucesso.
    /// </summary>
    void RegistrarCadastro();
}
