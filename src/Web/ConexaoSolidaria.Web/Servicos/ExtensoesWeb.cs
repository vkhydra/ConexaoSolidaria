namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Registro dos serviços da interface: clientes das APIs, sessão e manipulador do JWT.
/// </summary>
public static class ExtensoesWeb
{
    /// <summary>
    /// Registra os clientes da Identity.Api (<c>Api:Identity</c>) e da Campanhas.Api (<c>Api:Campanhas</c>), o manipulador que
    /// anexa o JWT e a sessão do usuário.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">O endereço de uma das APIs não foi configurado.</exception>
    public static IServiceCollection AddServicosDaApi(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var identity = configuracao["Api:Identity"] ?? throw new InvalidOperationException("Api:Identity não foi configurado.");
        var campanhas = configuracao["Api:Campanhas"] ?? throw new InvalidOperationException("Api:Campanhas não foi configurado.");

        servicos.AddTransient<AnexarJwtHandler>();
        servicos.AddScoped<ISessaoDoUsuario, SessaoDoUsuario>();
        servicos.AddSingleton<IServicoDeSessao, ServicoDeSessao>();
        servicos.AddHttpClient<IClienteIdentity, ClienteIdentity>(cliente => cliente.BaseAddress = new Uri(identity)).AddHttpMessageHandler<AnexarJwtHandler>();
        servicos.AddHttpClient<IClienteCampanhas, ClienteCampanhas>(cliente => cliente.BaseAddress = new Uri(campanhas)).AddHttpMessageHandler<AnexarJwtHandler>();
        return servicos;
    }
}
