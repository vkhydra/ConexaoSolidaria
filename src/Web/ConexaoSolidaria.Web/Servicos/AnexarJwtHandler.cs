using System.Net.Http.Headers;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Anexa o JWT do usuário no cabeçalho <c>Authorization</c> das chamadas às APIs. O token chega pelas opções da requisição
/// (<see cref="ChaveDoToken"/>), colocado pelo cliente que conhece a sessão do circuito; assim o manipulador, que o
/// <see cref="IHttpClientFactory"/> cria em outro escopo, não precisa acessar a sessão.
/// </summary>
public sealed class AnexarJwtHandler : DelegatingHandler
{
    /// <summary>
    /// Chave da opção da requisição que carrega o JWT.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> ChaveDoToken = new("conexao.jwt");

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Options.TryGetValue(ChaveDoToken, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
