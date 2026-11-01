using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notificacoes.Function.Recibos;
using StackExchange.Redis;

namespace Notificacoes.Function;

/// <summary>
/// Registro dos serviços da função de notificações.
/// </summary>
public static class ExtensoesNotificacoes
{
    /// <summary>
    /// Registra a conexão com o Redis (<c>ConnectionStrings:Redis</c>, aberta só no primeiro uso), o deduplicador e o emissor de recibos.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da função (variáveis de ambiente).</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A conexão do Redis não foi configurada.</exception>
    public static IServiceCollection AddNotificacoes(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var redis = configuracao.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis não foi configurada.");

        servicos.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));
        servicos.AddSingleton<IDeduplicadorRecibos, DeduplicadorRecibosRedis>();
        servicos.AddSingleton<EmissorRecibo>();
        return servicos;
    }
}
