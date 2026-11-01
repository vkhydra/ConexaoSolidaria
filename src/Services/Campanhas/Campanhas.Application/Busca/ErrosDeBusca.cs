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
    /// Código do erro de reindexação pedida enquanto outra ainda roda.
    /// </summary>
    public const string CodigoReindexacaoEmAndamento = "busca.reindexacao_em_andamento";

    /// <summary>
    /// Obtém o erro de busca indisponível.
    /// </summary>
    public static Erro Indisponivel { get; } = new(CodigoIndisponivel, "A busca está temporariamente indisponível.");

    /// <summary>
    /// Obtém o erro de reindexação já em andamento.
    /// </summary>
    public static Erro ReindexacaoEmAndamento { get; } = new(CodigoReindexacaoEmAndamento, "Já existe uma reindexação em andamento.");
}
