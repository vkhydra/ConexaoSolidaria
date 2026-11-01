using System.Net;
using System.Text;

namespace ConexaoSolidaria.Web.UnitTests.Suporte;

/// <summary>
/// Manipulador HTTP de teste: devolve a resposta escolhida pelo teste e guarda as requisições recebidas.
/// </summary>
/// <param name="responder">Cria a resposta para cada requisição.</param>
public sealed class ManipuladorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    /// <summary>
    /// Obtém as requisições recebidas, na ordem, com o corpo já lido como texto.
    /// </summary>
    public List<RequisicaoRecebida> Requisicoes { get; } = [];

    /// <summary>
    /// Cria uma resposta JSON.
    /// </summary>
    /// <param name="status">Status HTTP.</param>
    /// <param name="json">Corpo JSON.</param>
    /// <returns>A resposta.</returns>
    public static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requisicoes.Add(new RequisicaoRecebida(request.Method, request.RequestUri!.AbsoluteUri, request.Headers.Authorization?.ToString(), request.Headers.TryGetValues("If-Match", out var ifMatch) ? ifMatch.Single() : null, request.Headers.TryGetValues("Idempotency-Key", out var chave) ? chave.Single() : null, corpo));
        return responder(request);
    }
}

/// <summary>
/// Uma requisição capturada pelo <see cref="ManipuladorFalso"/>.
/// </summary>
/// <param name="Metodo">Método HTTP.</param>
/// <param name="Uri">Endereço pedido.</param>
/// <param name="Autorizacao">Cabeçalho Authorization, se houve.</param>
/// <param name="IfMatch">Cabeçalho If-Match, se houve.</param>
/// <param name="ChaveDeIdempotencia">Cabeçalho Idempotency-Key, se houve.</param>
/// <param name="Corpo">Corpo da requisição, se houve.</param>
public sealed record RequisicaoRecebida(HttpMethod Metodo, string Uri, string? Autorizacao, string? IfMatch, string? ChaveDeIdempotencia, string? Corpo);
