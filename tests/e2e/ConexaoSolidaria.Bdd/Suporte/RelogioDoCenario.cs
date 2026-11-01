namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Relógio do cenário: devolve o instante que o cenário escolher, inclusive voltando no tempo, o que o
/// <c>FakeTimeProvider</c> não permite.
/// </summary>
public sealed class RelogioDoCenario : TimeProvider
{
    /// <summary>
    /// Instante atual do cenário.
    /// </summary>
    private DateTimeOffset _agora = new(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Fixa o instante atual do cenário.
    /// </summary>
    /// <param name="agora">Novo instante.</param>
    public void Definir(DateTimeOffset agora)
    {
        _agora = agora;
    }

    /// <summary>
    /// Avança o instante atual do cenário.
    /// </summary>
    /// <param name="intervalo">Tempo a avançar.</param>
    public void Avancar(TimeSpan intervalo)
    {
        _agora += intervalo;
    }

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow()
    {
        return _agora;
    }
}
