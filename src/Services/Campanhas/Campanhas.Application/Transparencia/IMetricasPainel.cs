namespace Campanhas.Application.Transparencia;

/// <summary>
/// Resultado de uma consulta ao cache do painel, usado como rótulo de baixa cardinalidade da métrica.
/// </summary>
public enum ResultadoCache
{
    /// <summary>
    /// O painel estava no cache.
    /// </summary>
    Acerto,

    /// <summary>
    /// O painel não estava no cache e foi lido do banco.
    /// </summary>
    Falta,

    /// <summary>
    /// O cache falhou (por exemplo, Redis fora do ar).
    /// </summary>
    Erro,
}

/// <summary>
/// Porta da métrica <c>painel_cache_total{resultado}</c>.
/// </summary>
public interface IMetricasPainel
{
    /// <summary>
    /// Conta uma consulta ao cache do painel.
    /// </summary>
    /// <param name="resultado">Resultado da consulta.</param>
    void Registrar(ResultadoCache resultado);
}
