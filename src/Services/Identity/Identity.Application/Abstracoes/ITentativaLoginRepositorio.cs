using Identity.Domain.Autenticacao;

namespace Identity.Application.Abstracoes;

/// <summary>
/// Porta de persistência do controle de tentativas de login.
/// </summary>
public interface ITentativaLoginRepositorio
{
    /// <summary>
    /// Busca o controle de tentativas de um e-mail pelo hash dele.
    /// </summary>
    /// <param name="emailHash">Hash (HMAC) do e-mail normalizado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O controle ou nulo se o e-mail nunca falhou.</returns>
    Task<TentativaLogin?> ObterAsync(byte[] emailHash, CancellationToken cancelamento);

    /// <summary>
    /// Grava o controle de tentativas, criando-o se ainda não existir.
    /// </summary>
    /// <param name="tentativa">Controle a gravar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o controle foi gravado.</returns>
    Task SalvarAsync(TentativaLogin tentativa, CancellationToken cancelamento);
}
