namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Chamadas à Identity.Api usadas pela interface: login, cadastro e direitos do titular.
/// </summary>
public interface IClienteIdentity
{
    /// <summary>
    /// Autentica o usuário (<c>POST /api/v1/auth/login</c>).
    /// </summary>
    /// <param name="email">E-mail.</param>
    /// <param name="senha">Senha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O JWT e o papel, ou o erro (401 credenciais inválidas, 429 bloqueio).</returns>
    Task<ResultadoApi<LoginResposta>> LoginAsync(string email, string senha, CancellationToken cancelamento = default);

    /// <summary>
    /// Cadastra um doador (<c>POST /api/v1/doadores</c>).
    /// </summary>
    /// <param name="cadastro">Dados do cadastro.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O doador criado, ou o erro (400 validação, 409 e-mail ou CPF em uso).</returns>
    Task<ResultadoApi<DoadorResposta>> CadastrarAsync(CadastroDoadorRequest cadastro, CancellationToken cancelamento = default);

    /// <summary>
    /// Obtém os dados do titular logado (<c>GET /api/v1/doadores/me</c>).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os dados do titular, ou o erro.</returns>
    Task<ResultadoApi<MeusDadosResposta>> ObterMeusDadosAsync(CancellationToken cancelamento = default);

    /// <summary>
    /// Anonimiza o cadastro do titular logado (<c>DELETE /api/v1/doadores/me</c>).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Sucesso (204) ou o erro.</returns>
    Task<ResultadoApi> ExcluirMeusDadosAsync(CancellationToken cancelamento = default);
}

/// <summary>
/// Cliente HTTP da Identity.Api.
/// </summary>
/// <param name="http">Cliente HTTP apontado para a Identity.Api.</param>
/// <param name="sessao">Sessão do usuário.</param>
public sealed class ClienteIdentity(HttpClient http, ISessaoDoUsuario sessao) : ClienteApiBase(http, sessao), IClienteIdentity
{
    /// <inheritdoc/>
    public Task<ResultadoApi<LoginResposta>> LoginAsync(string email, string senha, CancellationToken cancelamento = default)
    {
        return EnviarAsync<LoginResposta>(HttpMethod.Post, "api/v1/auth/login", new { email, senha }, false, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<DoadorResposta>> CadastrarAsync(CadastroDoadorRequest cadastro, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(cadastro);

        return EnviarAsync<DoadorResposta>(HttpMethod.Post, "api/v1/doadores", cadastro, false, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi<MeusDadosResposta>> ObterMeusDadosAsync(CancellationToken cancelamento = default)
    {
        return EnviarAsync<MeusDadosResposta>(HttpMethod.Get, "api/v1/doadores/me", null, true, cancelamento);
    }

    /// <inheritdoc/>
    public Task<ResultadoApi> ExcluirMeusDadosAsync(CancellationToken cancelamento = default)
    {
        return EnviarSemConteudoAsync(HttpMethod.Delete, "api/v1/doadores/me", null, true, cancelamento);
    }
}
