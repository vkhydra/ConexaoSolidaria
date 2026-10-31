using Campanhas.Domain.Comum;

namespace Campanhas.Application.Busca;

/// <summary>
/// Erros de negócio da busca de campanhas.
/// </summary>
public static class ErrosDeBusca
{
    /// <summary>
    /// Código do erro de busca fora do ar.
    /// </summary>
    public const string CodigoIndisponivel = "busca.indisponivel";

    /// <summary>
    /// Obtém o erro de busca indisponível.
    /// </summary>
    public static Erro Indisponivel { get; } = new(CodigoIndisponivel, "A busca está temporariamente indisponível.");
}
