namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta das métricas de doadores: <c>doadores_cadastrados_total</c> e <c>titulares_anonimizados_total</c>.
/// </summary>
public interface IMetricasDoadores
{
    /// <summary>
    /// Conta um doador cadastrado com sucesso.
    /// </summary>
    void RegistrarCadastro();

    /// <summary>
    /// Conta um titular que teve o cadastro anonimizado.
    /// </summary>
    void RegistrarAnonimizacao();
}
