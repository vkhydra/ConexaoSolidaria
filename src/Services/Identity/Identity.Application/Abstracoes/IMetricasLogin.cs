namespace Identity.Application.Abstracoes;

/// <summary>
/// Resultado de uma tentativa de login, usado como rótulo de baixa cardinalidade da métrica.
/// </summary>
public enum ResultadoLogin
{
    /// <summary>
    /// Credenciais corretas e token emitido.
    /// </summary>
    Sucesso,

    /// <summary>
    /// E-mail inexistente, senha errada ou usuário anonimizado.
    /// </summary>
    Falha,

    /// <summary>
    /// Tentativa recusada porque o e-mail está bloqueado por excesso de falhas.
    /// </summary>
    Bloqueado,
}

/// <summary>
/// Porta da métrica <c>logins_total{resultado}</c>.
/// </summary>
public interface IMetricasLogin
{
    /// <summary>
    /// Conta uma tentativa de login.
    /// </summary>
    /// <param name="resultado">Resultado da tentativa.</param>
    void Registrar(ResultadoLogin resultado);
}
