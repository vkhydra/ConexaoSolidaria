namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Chamadas à Campanhas.Api usadas pela interface: painel, busca, gestão e doação.
/// </summary>
public interface IClienteCampanhas
{
    /// <summary>
    /// Obtém o painel público de transparência (<c>GET /api/v1/transparencia/campanhas</c>).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As campanhas ativas, ou o erro.</returns>
    Task<ResultadoApi<IReadOnlyList<PainelItem>>> ObterPainelAsync(CancellationToken cancelamento = default);

    /// <summary>
    /// Busca campanhas (<c>GET /api/v1/campanhas/busca</c>).
    /// </summary>
    /// <param name="texto">Texto procurado.</param>
    /// <param name="pagina">Número da página.</param>
    /// <param name="tamanhoPagina">Itens por página.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página de resultados, ou o erro (400 validação, 503 busca indisponível).</returns>
    Task<ResultadoApi<BuscaResposta>> BuscarAsync(string texto, int pagina = 1, int tamanhoPagina = 10, CancellationToken cancelamento = default);

    /// <summary>
    /// Lista as campanhas da gestão (<c>GET /api/v1/campanhas</c>).
    /// </summary>
    /// <param name="status">Filtro de status, ou nulo para todos.</param>
    /// <param name="pagina">Número da página.</param>
    /// <param name="tamanhoPagina">Itens por página.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página de campanhas, ou o erro.</returns>
    Task<ResultadoApi<PaginaDeCampanhas>> ListarAsync(string? status, int pagina = 1, int tamanhoPagina = 20, CancellationToken cancelamento = default);

    /// <summary>
    /// Obtém uma campanha com a versão para edição (<c>GET /api/v1/campanhas/{id}</c>).
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha e o ETag, ou o erro (404 inexistente).</returns>
    Task<ResultadoApi<CampanhaComVersao>> ObterAsync(Guid id, CancellationToken cancelamento = default);

    /// <summary>
    /// Cria uma campanha (<c>POST /api/v1/campanhas</c>).
    /// </summary>
    /// <param name="campanha">Dados da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha criada, ou o erro (400 e 422).</returns>
    Task<ResultadoApi<CampanhaGestao>> CriarAsync(CampanhaRequest campanha, CancellationToken cancelamento = default);

    /// <summary>
    /// Edita uma campanha ativa (<c>PUT /api/v1/campanhas/{id}</c>, com <c>If-Match</c>).
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="versao">ETag lido com a campanha.</param>
    /// <param name="campanha">Novos dados.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha atualizada, ou o erro (409 conflito de versão, 422 não editável).</returns>
    Task<ResultadoApi<CampanhaGestao>> AtualizarAsync(Guid id, string versao, CampanhaRequest campanha, CancellationToken cancelamento = default);

    /// <summary>
    /// Conclui ou cancela uma campanha (<c>PATCH /api/v1/campanhas/{id}/status</c>).
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="status">Status de destino (<c>Concluida</c> ou <c>Cancelada</c>).</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Sucesso ou o erro (422 transição inválida).</returns>
    Task<ResultadoApi> AlterarStatusAsync(Guid id, string status, CancellationToken cancelamento = default);

    /// <summary>
    /// Doa para uma campanha (<c>POST /api/v1/doacoes</c>, com <c>Idempotency-Key</c>).
    /// </summary>
    /// <param name="campanhaId">Campanha beneficiada.</param>
    /// <param name="valor">Valor da doação.</param>
    /// <param name="chaveDeIdempotencia">Chave única da tentativa, reaproveitada se a mesma tentativa for repetida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A doação aceita (202), ou o erro (400, 404, 422).</returns>
    Task<ResultadoApi<DoacaoAceita>> DoarAsync(Guid campanhaId, decimal valor, string chaveDeIdempotencia, CancellationToken cancelamento = default);
}

/// <summary>
/// Cliente HTTP da Campanhas.Api.
/// </summary>
/// <param name="http">Cliente HTTP apontado para a Campanhas.Api.</param>
/// <param name="sessao">Sessão do usuário.</param>
public sealed class ClienteCampanhas(HttpClient http, ISessaoDoUsuario sessao) : ClienteApiBase(http, sessao), IClienteCampanhas
{
    /// <inheritdoc/>
    public Task<ResultadoApi<IReadOnlyList<PainelItem>>> ObterPainelAsync(CancellationToken cancelamento = default)
    {
        return ExecutarAsync<IReadOnlyList<PainelItem>>(
            HttpMethod.Get,
            "api/v1/transparencia/campanhas",
            null,
            false,
            null,
            async resposta => (await LerAsync<List<PainelItem>>(resposta, cancelamento))!,
            cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<BuscaResposta>> BuscarAsync(string texto, int pagina = 1, int tamanhoPagina = 10, CancellationToken cancelamento = default)
    {
        var consulta = $"q={Uri.EscapeDataString(texto ?? string.Empty)}&page={pagina}&pageSize={tamanhoPagina}";
        return EnviarAsync<BuscaResposta>(HttpMethod.Get, $"api/v1/campanhas/busca?{consulta}", null, false, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<PaginaDeCampanhas>> ListarAsync(string? status, int pagina = 1, int tamanhoPagina = 20, CancellationToken cancelamento = default)
    {
        var filtro = string.IsNullOrWhiteSpace(status) ? string.Empty : $"status={Uri.EscapeDataString(status)}&";
        return EnviarAsync<PaginaDeCampanhas>(HttpMethod.Get, $"api/v1/campanhas?{filtro}page={pagina}&pageSize={tamanhoPagina}", null, true, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<CampanhaComVersao>> ObterAsync(Guid id, CancellationToken cancelamento = default)
    {
        return ExecutarAsync(
            HttpMethod.Get,
            $"api/v1/campanhas/{id:D}",
            null,
            true,
            null,
            async resposta => new CampanhaComVersao((await LerAsync<CampanhaGestao>(resposta, cancelamento))!, resposta.Headers.ETag?.Tag ?? string.Empty),
            cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<CampanhaGestao>> CriarAsync(CampanhaRequest campanha, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        return EnviarAsync<CampanhaGestao>(HttpMethod.Post, "api/v1/campanhas", campanha, true, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<CampanhaGestao>> AtualizarAsync(Guid id, string versao, CampanhaRequest campanha, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        return ExecutarAsync(
            HttpMethod.Put,
            $"api/v1/campanhas/{id:D}",
            campanha,
            true,
            new Dictionary<string, string> { ["If-Match"] = versao },
            async resposta => (await LerAsync<CampanhaGestao>(resposta, cancelamento))!,
            cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi> AlterarStatusAsync(Guid id, string status, CancellationToken cancelamento = default)
    {
        return EnviarSemConteudoAsync(HttpMethod.Patch, $"api/v1/campanhas/{id:D}/status", new { status }, true, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<DoacaoAceita>> DoarAsync(Guid campanhaId, decimal valor, string chaveDeIdempotencia, CancellationToken cancelamento = default)
    {
        return ExecutarAsync(
            HttpMethod.Post,
            "api/v1/doacoes",
            new { idCampanha = campanhaId, valorDoacao = valor },
            true,
            new Dictionary<string, string> { ["Idempotency-Key"] = chaveDeIdempotencia },
            async resposta => (await LerAsync<DoacaoAceita>(resposta, cancelamento))!,
            cancelamento);
    }

    /// <summary>
    /// Lê o corpo JSON de uma resposta de sucesso.
    /// </summary>
    /// <typeparam name="T">Tipo esperado.</typeparam>
    /// <param name="resposta">Resposta HTTP.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O valor lido.</returns>
    private static Task<T?> LerAsync<T>(HttpResponseMessage resposta, CancellationToken cancelamento)
    {
        return System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(resposta.Content, OpcoesJson, cancelamento);
    }
}
