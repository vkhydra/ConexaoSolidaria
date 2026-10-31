using Campanhas.Domain.Comum;

namespace Campanhas.Application.Doacoes;

/// <summary>
/// Erros de negócio do recebimento de doações.
/// </summary>
public static class ErrosDeDoacao
{
    /// <summary>
    /// Código do erro de campanha que não aceita doação (concluída, cancelada ou fora do período).
    /// </summary>
    public const string CodigoCampanhaNaoAtiva = "doacao.campanha_nao_ativa";

    /// <summary>
    /// Código do erro de chave de idempotência reutilizada com outro corpo.
    /// </summary>
    public const string CodigoChaveConflitante = "doacao.chave_idempotencia_conflitante";

    /// <summary>
    /// Obtém o erro de campanha que não aceita doação.
    /// </summary>
    public static Erro CampanhaNaoAtiva { get; } = new(CodigoCampanhaNaoAtiva, "A campanha não está ativa para receber doações.");

    /// <summary>
    /// Obtém o erro de chave de idempotência reutilizada com outro corpo.
    /// </summary>
    public static Erro ChaveConflitante { get; } = new(CodigoChaveConflitante, "A Idempotency-Key já foi usada com outra doação.");
}
