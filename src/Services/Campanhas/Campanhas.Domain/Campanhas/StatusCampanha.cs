namespace Campanhas.Domain.Campanhas;

/// <summary>
/// Situação de uma campanha. Só <see cref="Ativa"/> recebe edição e doação; as outras são finais.
/// </summary>
public enum StatusCampanha
{
    /// <summary>
    /// Campanha em andamento.
    /// </summary>
    Ativa = 1,

    /// <summary>
    /// Campanha encerrada com sucesso pelo gestor.
    /// </summary>
    Concluida = 2,

    /// <summary>
    /// Campanha cancelada pelo gestor.
    /// </summary>
    Cancelada = 3,
}
