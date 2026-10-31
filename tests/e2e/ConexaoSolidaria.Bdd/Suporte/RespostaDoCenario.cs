namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Guarda a última resposta HTTP do cenário, qualquer que seja o serviço que a deu, para que os passos
/// "recebe status" e "o código do erro é" valham para a Identity e para as Campanhas.
/// </summary>
public sealed class RespostaDoCenario
{
    /// <summary>
    /// Obtém ou define a última resposta HTTP recebida pelo cenário.
    /// </summary>
    public HttpResponseMessage? Resposta { get; set; }
}
