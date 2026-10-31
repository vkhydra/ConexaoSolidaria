using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Seed;

/// <summary>
/// Cria o primeiro gestor da ONG na inicialização, a partir de <c>Seed__GestorEmail</c> e <c>Seed__GestorSenha</c>,
/// só se ainda não existir nenhum gestor. Não derruba a API: falhas viram log, sem e-mail nem senha.
/// </summary>
/// <param name="escopos">Fábrica de escopos, porque o serviço é singleton e os repositórios são scoped.</param>
/// <param name="configuracao">Configuração da aplicação.</param>
/// <param name="relogio">Relógio da aplicação.</param>
/// <param name="log">Logger do serviço.</param>
public sealed partial class SeedGestorHostedService(
    IServiceScopeFactory escopos,
    IConfiguration configuracao,
    TimeProvider relogio,
    ILogger<SeedGestorHostedService> log) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuracao["Seed:GestorEmail"];
        var senha = configuracao["Seed:GestorSenha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            LogSeedNaoConfigurado(log);
            return;
        }

        var emailCriado = Email.Criar(email);
        if (emailCriado.EhFalha)
        {
            LogEmailInvalido(log);
            return;
        }

        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var usuarios = escopo.ServiceProvider.GetRequiredService<IUsuarioRepositorio>();
            if (await usuarios.ExisteGestorAsync(cancellationToken))
            {
                return;
            }

            var hash = escopo.ServiceProvider.GetRequiredService<IHasherSenha>().Hash(senha);
            var gestor = Usuario.CriarGestor(emailCriado.Valor, hash, relogio.GetUtcNow()).Valor;
            await usuarios.AdicionarAsync(gestor, cancellationToken);
            LogGestorCriado(log, gestor.Id);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            LogFalhaNoSeed(log, excecao.GetType().Name);
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registra que as variáveis do gestor inicial não foram definidas.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Seed__GestorEmail ou Seed__GestorSenha ausente: nenhum gestor foi criado.")]
    private static partial void LogSeedNaoConfigurado(ILogger log);

    /// <summary>
    /// Registra que o e-mail do gestor inicial não tem formato válido, sem escrever o valor.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Seed__GestorEmail não é um e-mail válido: nenhum gestor foi criado.")]
    private static partial void LogEmailInvalido(ILogger log);

    /// <summary>
    /// Registra a criação do gestor inicial, só com o identificador.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    /// <param name="usuarioId">Identificador do gestor criado.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Gestor inicial criado: {UsuarioId}.")]
    private static partial void LogGestorCriado(ILogger log, UsuarioId usuarioId);

    /// <summary>
    /// Registra a falha do seed, só com o tipo da exceção para não vazar dados.
    /// </summary>
    /// <param name="log">Logger do serviço.</param>
    /// <param name="tipoDaExcecao">Nome do tipo da exceção.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao criar o gestor inicial ({TipoDaExcecao}).")]
    private static partial void LogFalhaNoSeed(ILogger log, string tipoDaExcecao);
}
