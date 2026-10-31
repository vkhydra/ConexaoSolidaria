using System.Security.Cryptography;
using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Identity.IntegrationTests.Suporte;

/// <summary>
/// Sobe a Identity.Api inteira (pipeline real, PostgreSQL em container e chaves JWT geradas na hora) para os testes de HTTP.
/// </summary>
public sealed class IdentityApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Banco PostgreSQL em container, com as migrations aplicadas.
    /// </summary>
    private readonly BancoIdentityFixture _banco = new();

    /// <summary>
    /// Par de chaves RSA do JWT, gerado só para os testes.
    /// </summary>
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>
    /// Sink que guarda os eventos de log para os testes conferirem que não há dado pessoal.
    /// </summary>
    private readonly SinkDeTeste _logs = new();

    /// <summary>
    /// Obtém os textos de todos os logs emitidos pela API, já renderizados com as propriedades.
    /// </summary>
    public IReadOnlyList<string> TextosDeLog => _logs.Textos();

    /// <summary>
    /// Obtém a chave pública em PEM, para validar os tokens emitidos.
    /// </summary>
    public string ChavePublicaPem => _rsa.ExportSubjectPublicKeyInfoPem();

    /// <summary>
    /// Sobe o banco antes da API.
    /// </summary>
    /// <returns>Tarefa que termina com o banco pronto.</returns>
    public async ValueTask InitializeAsync()
    {
        await _banco.InitializeAsync();
    }

    /// <summary>
    /// Grava um gestor com a senha informada e devolve o e-mail único criado.
    /// </summary>
    /// <param name="senha">Senha em texto do gestor.</param>
    /// <returns>E-mail do gestor gravado.</returns>
    public async Task<string> CriarGestorAsync(string senha)
    {
        var email = $"gestor-{Guid.NewGuid():N}@exemplo.com";
        await using var escopo = Services.CreateAsyncScope();
        var hash = escopo.ServiceProvider.GetRequiredService<IHasherSenha>().Hash(senha);
        var gestor = Usuario.CriarGestor(Email.Criar(email).Valor, hash, DateTimeOffset.UtcNow).Valor;
        await escopo.ServiceProvider.GetRequiredService<IUsuarioRepositorio>().AdicionarAsync(gestor, CancellationToken.None);
        return email;
    }

    /// <summary>
    /// Lê, direto do banco, o CPF cifrado de um usuário.
    /// </summary>
    /// <param name="email">E-mail normalizado do usuário.</param>
    /// <returns>Os bytes da coluna <c>cpf_cifrado</c>.</returns>
    public async Task<byte[]?> LerCpfCifradoAsync(string email)
    {
        await using var contexto = _banco.CriarContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Email == Email.Criar(email).Valor);
        return contexto.Entry(usuario).Property<byte[]?>("CpfCifrado").CurrentValue;
    }

    /// <summary>
    /// Conta os gestores gravados no banco.
    /// </summary>
    /// <returns>Quantidade de usuários com o papel de gestor.</returns>
    public async Task<int> ContarGestoresAsync()
    {
        await using var contexto = _banco.CriarContexto();
        return await contexto.Usuarios.CountAsync(u => u.Role == Role.GestorONG);
    }

    /// <summary>
    /// Libera a API, o banco e a chave.
    /// </summary>
    /// <returns>Tarefa que termina com tudo liberado.</returns>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _banco.DisposeAsync();
        _rsa.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Identity", _banco.StringConexao);
        builder.UseSetting("Autenticacao:ChaveHashEmail", "chave-hash-email-de-teste");
        builder.UseSetting("Cpf:ChaveCifra", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Cpf:ChaveHash", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Jwt:ChavePrivadaPem", _rsa.ExportRSAPrivateKeyPem());
        builder.UseSetting("Jwt:ChavePublicaPem", ChavePublicaPem);
        builder.ConfigureServices(servicos => servicos.AddSingleton<ILogEventSink>(_logs));
    }

    /// <summary>
    /// Sink do Serilog que guarda os eventos em memória.
    /// </summary>
    private sealed class SinkDeTeste : ILogEventSink
    {
        /// <summary>
        /// Eventos recebidos, protegidos por trava porque a API escreve de várias threads.
        /// </summary>
        private readonly List<LogEvent> _eventos = [];

        /// <inheritdoc/>
        public void Emit(LogEvent logEvent)
        {
            lock (_eventos)
            {
                _eventos.Add(logEvent);
            }
        }

        /// <summary>
        /// Renderiza cada evento com mensagem, propriedades e exceção.
        /// </summary>
        /// <returns>Um texto por evento.</returns>
        public List<string> Textos()
        {
            lock (_eventos)
            {
                return [.. _eventos.Select(e => e.RenderMessage() + " " + string.Join(' ', e.Properties.Select(p => p.Value.ToString())) + " " + e.Exception)];
            }
        }
    }
}
