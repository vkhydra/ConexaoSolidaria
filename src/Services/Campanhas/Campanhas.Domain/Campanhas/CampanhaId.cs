namespace Campanhas.Domain.Campanhas;

/// <summary>
/// Identificador de uma campanha: um GUID versão 7, ordenável por data de criação.
/// </summary>
/// <param name="Valor">GUID da campanha.</param>
public readonly record struct CampanhaId(Guid Valor)
{
    /// <summary>
    /// Cria um identificador novo.
    /// </summary>
    /// <returns>Um identificador GUID v7.</returns>
    public static CampanhaId Novo()
    {
        return new CampanhaId(Guid.CreateVersion7());
    }

    /// <summary>
    /// Devolve o GUID em texto.
    /// </summary>
    /// <returns>O GUID da campanha.</returns>
    public override string ToString()
    {
        return Valor.ToString();
    }
}
