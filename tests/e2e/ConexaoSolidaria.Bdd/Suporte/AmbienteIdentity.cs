extern alias IdentityApi;

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Ambiente de um cenário: Identity.Api real em memória, banco PostgreSQL exclusivo e chaves JWT geradas na hora.
/// Os passos dos cenários usam este objeto sem conhecer detalhes de HTTP.
/// </summary>
/// <param name="respostaDoCenario">Guarda a última resposta do cenário, compartilhada com os outros serviços.</param>
public sealed class AmbienteIdentity(RespostaDoCenario respostaDoCenario) : IDisposable
{
    /// <summary>
    /// Par de chaves RSA do JWT, só para o cenário.
    /// </summary>
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>
    /// APIs iniciadas pelo cenário, descartadas no fim.
    /// </summary>
    private readonly List<WebApplicationFactory<IdentityApi::Program>> _apis = [];

    /// <summary>
    /// String de conexão do banco do cenário; nula até a primeira API iniciar.
    /// </summary>
    private string? _conexao;

    /// <summary>
    /// Cliente da instância principal da API, criado sob demanda.
    /// </summary>
    private HttpClient? _cliente;

    /// <summary>
    /// Obtém ou define o e-mail do gestor inicial; nulo desliga o seed.
    /// </summary>
    public string? SeedEmail { get; set; }

    /// <summary>
    /// Obtém ou define a senha do gestor inicial.
    /// </summary>
    public string? SeedSenha { get; set; }

    /// <summary>
    /// Obtém a última resposta HTTP recebida pelo cenário.
    /// </summary>
    public HttpResponseMessage? Resposta
    {
        get => respostaDoCenario.Resposta;
        private set => respostaDoCenario.Resposta = value;
    }

    /// <summary>
    /// Obtém a chave pública do JWT em PEM, com a qual os outros serviços do cenário validam os tokens da Identity.
    /// </summary>
    public string ChavePublicaPem => _rsa.ExportSubjectPublicKeyInfoPem();

    /// <summary>
    /// Entra com o e-mail e a senha informados, sem mexer no cabeçalho de autorização, e devolve o token.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="senha">Senha do usuário.</param>
    /// <returns>O <c>accessToken</c> devolvido pelo login.</returns>
    public async Task<string> ObterTokenAsync(string email, string senha)
    {
        await LoginAsync(email, senha);
        return (await LerCampoAsync("accessToken"))!;
    }

    /// <summary>
    /// Cria o gestor inicial (seed) se preciso e devolve o token dele.
    /// </summary>
    /// <returns>O token do gestor.</returns>
    public async Task<string> ObterTokenDoGestorAsync()
    {
        SeedEmail ??= "gestor.bdd@exemplo.com";
        SeedSenha ??= "SenhaGestor@2026";
        return await ObterTokenAsync(SeedEmail, SeedSenha);
    }

    /// <summary>
    /// Cadastra um doador novo e devolve o token dele.
    /// </summary>
    /// <returns>O token do doador.</returns>
    public async Task<string> ObterTokenDeDoadorAsync()
    {
        const string email = "doador.bdd@exemplo.com";
        const string senha = "Senha@123";

        // O gestor do seed só existe se as variáveis estiverem definidas antes de a API subir; um cenário que começa pelo
        // doador e depois usa o gestor (por exemplo, "gestor não doa") precisa dele.
        SeedEmail ??= "gestor.bdd@exemplo.com";
        SeedSenha ??= "SenhaGestor@2026";
        await CadastrarAsync("Maria Silva", email, "529.982.247-25", senha, aceite: true);
        return await ObterTokenAsync(email, senha);
    }

    /// <summary>
    /// Obtém ou define o e-mail do último doador cadastrado pelo cenário.
    /// </summary>
    public string? UltimoEmail { get; set; }

    /// <summary>
    /// Obtém ou define a senha do último doador cadastrado pelo cenário.
    /// </summary>
    public string? UltimaSenha { get; set; }

    /// <summary>
    /// Inicia uma instância da Identity.Api ligada ao banco do cenário, criando o banco e aplicando as migrations na primeira vez.
    /// </summary>
    /// <returns>Cliente HTTP da instância iniciada.</returns>
    public async Task<HttpClient> IniciarApiAsync()
    {
        if (_conexao is null)
        {
            _conexao = await ServidorPostgres.CriarBancoAsync();
            await using var contexto = CriarContexto();
            await contexto.Database.MigrateAsync();
        }

        var api = new WebApplicationFactory<IdentityApi::Program>().WithWebHostBuilder(ConfigurarApi);
        _apis.Add(api);
        return api.CreateClient();
    }

    /// <summary>
    /// Faz login e guarda a resposta.
    /// </summary>
    /// <param name="email">E-mail digitado.</param>
    /// <param name="senha">Senha digitada.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task LoginAsync(string email, string senha)
    {
        var cliente = await ObterClienteAsync();
        Resposta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
    }

    /// <summary>
    /// Envia um cadastro de doador e guarda a resposta.
    /// </summary>
    /// <param name="nome">Nome completo.</param>
    /// <param name="email">E-mail.</param>
    /// <param name="cpf">CPF, com ou sem máscara.</param>
    /// <param name="senha">Senha.</param>
    /// <param name="aceite">Se a política de privacidade foi aceita.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task CadastrarAsync(string nome, string email, string cpf, string senha, bool aceite)
    {
        var cliente = await ObterClienteAsync();
        Resposta = await cliente.PostAsJsonAsync(
            "/api/v1/doadores",
            new { nomeCompleto = nome, email, cpf, senha, aceitePoliticaPrivacidade = aceite });
    }

    /// <summary>
    /// Faz login e passa a enviar o token em todas as chamadas seguintes do cenário.
    /// </summary>
    /// <param name="email">E-mail do doador.</param>
    /// <param name="senha">Senha do doador.</param>
    /// <returns>Tarefa que termina com o token em uso.</returns>
    public async Task AutenticarAsync(string email, string senha)
    {
        await LoginAsync(email, senha);
        var token = await LerCampoAsync("accessToken");
        var cliente = await ObterClienteAsync();
        cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Consulta <c>GET /api/v1/doadores/me</c> com o token em uso e guarda a resposta.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task ConsultarMeusDadosAsync()
    {
        Resposta = await (await ObterClienteAsync()).GetAsync("/api/v1/doadores/me");
    }

    /// <summary>
    /// Chama <c>DELETE /api/v1/doadores/me</c> com o token em uso e guarda a resposta.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task ExcluirMeusDadosAsync()
    {
        Resposta = await (await ObterClienteAsync()).DeleteAsync("/api/v1/doadores/me");
    }

    /// <summary>
    /// Lê um campo de texto dentro de um objeto do JSON da última resposta.
    /// </summary>
    /// <param name="objeto">Nome do objeto em camelCase.</param>
    /// <param name="campo">Nome do campo dentro do objeto.</param>
    /// <returns>O valor em texto, ou nulo se não existe.</returns>
    public async Task<string?> LerCampoAninhadoAsync(string objeto, string campo)
    {
        var texto = await Resposta!.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);
        return documento.RootElement.TryGetProperty(objeto, out var filho) && filho.TryGetProperty(campo, out var valor)
            ? valor.ToString()
            : null;
    }

    /// <summary>
    /// Lê um campo de texto do JSON da última resposta.
    /// </summary>
    /// <param name="campo">Nome do campo em camelCase.</param>
    /// <returns>O valor do campo, ou nulo se ele não existe.</returns>
    public async Task<string?> LerCampoAsync(string campo)
    {
        var texto = await Resposta!.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);
        return documento.RootElement.TryGetProperty(campo, out var valor) ? valor.GetString() : null;
    }

    /// <summary>
    /// Lê o payload do <c>accessToken</c> da última resposta de login, já decodificado em JSON.
    /// </summary>
    /// <returns>O texto JSON do payload do token.</returns>
    public async Task<string> LerPayloadDoTokenAsync()
    {
        var token = await LerCampoAsync("accessToken");
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        return Base64UrlEncoder.Decode(jwt.EncodedPayload);
    }

    /// <summary>
    /// Lê, direto do banco, o e-mail e o hash de senha de um usuário.
    /// </summary>
    /// <param name="email">E-mail normalizado.</param>
    /// <returns>O e-mail gravado e o hash da senha.</returns>
    public async Task<(string Email, string? SenhaHash)> LerUsuarioGravadoAsync(string email)
    {
        await using var contexto = CriarContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Email == Identity.Domain.Usuarios.Email.Criar(email).Valor);
        return (usuario.Email.Valor, usuario.SenhaHash);
    }

    /// <summary>
    /// Obtém o cliente HTTP da primeira instância da API, criando-a na primeira chamada.
    /// </summary>
    /// <returns>O cliente da instância compartilhada pelo cenário.</returns>
    private async Task<HttpClient> ObterClienteAsync()
    {
        return _cliente ??= await IniciarApiAsync();
    }

    /// <summary>
    /// Lê o título do problema (<c>application/problem+json</c>) da última resposta.
    /// </summary>
    /// <returns>O título da resposta de erro.</returns>
    public async Task<string?> LerTituloAsync()
    {
        var corpo = await Resposta!.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("title").GetString();
    }

    /// <summary>
    /// Lista os papéis dos usuários que são gestores no banco do cenário.
    /// </summary>
    /// <returns>Os papéis encontrados, em texto.</returns>
    public async Task<IReadOnlyList<string>> ListarPapeisDeGestorAsync()
    {
        await using var contexto = CriarContexto();
        var papeis = await contexto.Usuarios
            .Where(u => u.Role == Role.GestorONG)
            .Select(u => u.Role)
            .ToListAsync();

        return [.. papeis.Select(p => p.ToString())];
    }

    /// <summary>
    /// Libera as APIs e a chave.
    /// </summary>
    public void Dispose()
    {
        _cliente?.Dispose();
        foreach (var api in _apis)
        {
            api.Dispose();
        }

        _rsa.Dispose();
    }

    /// <summary>
    /// Cria um contexto do EF ligado ao banco do cenário.
    /// </summary>
    /// <returns>Contexto que o chamador deve descartar.</returns>
    private IdentityDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(_conexao).Options;
        return new IdentityDbContext(opcoes);
    }

    /// <summary>
    /// Aplica as configurações do cenário à API.
    /// </summary>
    /// <param name="builder">Construtor do host da API.</param>
    private void ConfigurarApi(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Identity", _conexao);
        builder.UseSetting("Autenticacao:ChaveHashEmail", "chave-hash-email-do-bdd");
        builder.UseSetting("Cpf:ChaveCifra", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Cpf:ChaveHash", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Jwt:ChavePrivadaPem", _rsa.ExportRSAPrivateKeyPem());
        builder.UseSetting("Jwt:ChavePublicaPem", _rsa.ExportSubjectPublicKeyInfoPem());
        if (SeedEmail is not null)
        {
            builder.UseSetting("Seed:GestorEmail", SeedEmail);
            builder.UseSetting("Seed:GestorSenha", SeedSenha);
        }
    }
}
