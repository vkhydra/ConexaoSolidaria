using System.Text.Json;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Base dos clientes das APIs: monta a requisição, anexa o JWT quando pedido e traduz as respostas de erro
/// (<c>application/problem+json</c>) em <see cref="ErroApi"/>. Falhas de rede viram o erro 503 <c>servico.indisponivel</c>.
/// </summary>
/// <param name="http">Cliente HTTP já apontado para a base da API.</param>
/// <param name="sessao">Sessão do usuário, de onde vem o JWT.</param>
public abstract class ClienteApiBase(HttpClient http, ISessaoDoUsuario sessao)
{
    /// <summary>
    /// Opções de JSON: camelCase, como as APIs.
    /// </summary>
    protected static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Envia uma requisição e lê o corpo da resposta de sucesso como JSON.
    /// </summary>
    /// <typeparam name="T">Tipo do corpo esperado.</typeparam>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="caminho">Caminho relativo à base da API.</param>
    /// <param name="corpo">Corpo JSON da requisição, ou nulo.</param>
    /// <param name="autenticado">Se a chamada leva o JWT do usuário.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O valor lido ou o erro da API.</returns>
    protected Task<ResultadoApi<T>> EnviarAsync<T>(HttpMethod metodo, string caminho, object? corpo, bool autenticado, CancellationToken cancelamento)
    {
        return ExecutarAsync(
            metodo,
            caminho,
            corpo,
            autenticado,
            null,
            async resposta => (await resposta.Content.ReadFromJsonAsync<T>(OpcoesJson, cancelamento))!,
            cancelamento);
    }

    /// <summary>
    /// Envia uma requisição cuja resposta de sucesso não tem corpo (por exemplo, 204).
    /// </summary>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="caminho">Caminho relativo à base da API.</param>
    /// <param name="corpo">Corpo JSON da requisição, ou nulo.</param>
    /// <param name="autenticado">Se a chamada leva o JWT do usuário.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Sucesso ou o erro da API.</returns>
    protected async Task<ResultadoApi> EnviarSemConteudoAsync(HttpMethod metodo, string caminho, object? corpo, bool autenticado, CancellationToken cancelamento)
    {
        var resultado = await ExecutarAsync(metodo, caminho, corpo, autenticado, null, _ => Task.FromResult(true), cancelamento);
        return resultado.Sucesso ? ResultadoApi.Ok() : ResultadoApi.Falha(resultado.Erro!);
    }

    /// <summary>
    /// Envia uma requisição e deixa o chamador ler a resposta de sucesso (por exemplo, para ler cabeçalhos).
    /// </summary>
    /// <typeparam name="T">Tipo do valor produzido.</typeparam>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="caminho">Caminho relativo à base da API.</param>
    /// <param name="corpo">Corpo JSON da requisição, ou nulo.</param>
    /// <param name="autenticado">Se a chamada leva o JWT do usuário.</param>
    /// <param name="cabecalhos">Cabeçalhos extras (por exemplo, <c>If-Match</c>), ou nulo.</param>
    /// <param name="ler">Converte a resposta de sucesso no valor.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O valor produzido ou o erro da API.</returns>
    protected async Task<ResultadoApi<T>> ExecutarAsync<T>(
        HttpMethod metodo,
        string caminho,
        object? corpo,
        bool autenticado,
        IReadOnlyDictionary<string, string>? cabecalhos,
        Func<HttpResponseMessage, Task<T>> ler,
        CancellationToken cancelamento)
    {
        using var requisicao = new HttpRequestMessage(metodo, caminho);
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo, options: OpcoesJson);
        }

        if (autenticado && await sessao.ObterTokenAsync() is { } token)
        {
            requisicao.Options.Set(AnexarJwtHandler.ChaveDoToken, token);
        }

        foreach (var (nome, valor) in cabecalhos ?? new Dictionary<string, string>())
        {
            requisicao.Headers.TryAddWithoutValidation(nome, valor);
        }

        try
        {
            using var resposta = await http.SendAsync(requisicao, cancelamento);
            return resposta.IsSuccessStatusCode
                ? ResultadoApi.Ok(await ler(resposta))
                : ResultadoApi.Falha<T>(await LerErroAsync(resposta, cancelamento));
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException && !cancelamento.IsCancellationRequested)
        {
            return ResultadoApi.Falha<T>(new ErroApi(503, "servico.indisponivel", "O serviço está indisponível no momento. Tente novamente em instantes.", new Dictionary<string, string[]>(), null));
        }
    }

    /// <summary>
    /// Converte uma resposta de erro no <see cref="ErroApi"/>, mesmo quando o corpo não é um <c>problem+json</c>.
    /// </summary>
    /// <param name="resposta">Resposta com status de erro.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O erro da API.</returns>
    private static async Task<ErroApi> LerErroAsync(HttpResponseMessage resposta, CancellationToken cancelamento)
    {
        var status = (int)resposta.StatusCode;
        var texto = await resposta.Content.ReadAsStringAsync(cancelamento);
        try
        {
            using var documento = JsonDocument.Parse(texto);
            var raiz = documento.RootElement;
            var campos = new Dictionary<string, string[]>();
            if (raiz.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in erros.EnumerateObject())
                {
                    campos[campo.Name] = [.. campo.Value.EnumerateArray().Select(m => m.GetString() ?? string.Empty)];
                }
            }

            return new ErroApi(
                status,
                raiz.TryGetProperty("code", out var codigo) ? codigo.GetString() : null,
                raiz.TryGetProperty("title", out var titulo) ? titulo.GetString() ?? MensagemPadrao(status) : MensagemPadrao(status),
                campos,
                raiz.TryGetProperty("traceId", out var trace) ? trace.GetString() : null);
        }
        catch (Exception excecao) when (excecao is JsonException or InvalidOperationException)
        {
            return new ErroApi(status, null, MensagemPadrao(status), new Dictionary<string, string[]>(), null);
        }
    }

    /// <summary>
    /// Mensagem para um status sem <c>problem+json</c> legível.
    /// </summary>
    /// <param name="status">Status HTTP.</param>
    /// <returns>A mensagem em português.</returns>
    private static string MensagemPadrao(int status)
    {
        return status switch
        {
            401 => "Sua sessão expirou. Entre de novo.",
            403 => "Você não tem permissão para esta ação.",
            429 => "Muitas tentativas. Tente novamente em alguns minutos.",
            _ => "Não foi possível concluir a operação.",
        };
    }
}
