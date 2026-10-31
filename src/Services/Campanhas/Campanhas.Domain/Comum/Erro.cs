namespace Campanhas.Domain.Comum;

/// <summary>
/// Erro de regra de negócio, com código estável. O código é o que clientes e testes usam;
/// a mensagem é só para leitura humana.
/// </summary>
/// <param name="Codigo">Código estável do erro (por exemplo, <c>email.invalido</c>).</param>
/// <param name="Mensagem">Mensagem em português, sem dados pessoais.</param>
public sealed record Erro(string Codigo, string Mensagem)
{
    /// <summary>
    /// Obtém os erros por campo, quando o erro é de validação de várias entradas; senão, nulo.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Campos { get; init; }
}
