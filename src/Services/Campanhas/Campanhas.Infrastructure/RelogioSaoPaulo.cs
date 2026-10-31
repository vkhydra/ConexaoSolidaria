using Campanhas.Application.Abstracoes;

namespace Campanhas.Infrastructure;

/// <summary>
/// Relógio da aplicação no fuso <c>America/Sao_Paulo</c>, sobre o <see cref="TimeProvider"/> (que os testes controlam).
/// </summary>
/// <param name="relogio">Fonte de tempo.</param>
public sealed class RelogioSaoPaulo(TimeProvider relogio) : IRelogio
{
    /// <summary>
    /// Identificador IANA do fuso de São Paulo.
    /// </summary>
    public const string IdDoFuso = "America/Sao_Paulo";

    /// <summary>
    /// Fuso de São Paulo, resolvido uma vez.
    /// </summary>
    private static readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(IdDoFuso);

    /// <inheritdoc/>
    public DateOnly Hoje()
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(relogio.GetUtcNow(), _fuso).DateTime);
    }

    /// <inheritdoc/>
    public DateTimeOffset Agora()
    {
        return TimeZoneInfo.ConvertTime(relogio.GetUtcNow(), _fuso);
    }
}
