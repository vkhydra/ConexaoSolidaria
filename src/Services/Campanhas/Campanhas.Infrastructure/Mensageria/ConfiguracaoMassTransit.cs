using Campanhas.Infrastructure.Persistencia;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.Infrastructure.Mensageria;

/// <summary>
/// Configuração do MassTransit 8: Outbox transacional do EF Core sobre o PostgreSQL e transporte RabbitMQ.
/// </summary>
public static class ConfiguracaoMassTransit
{
    /// <summary>
    /// Liga o Outbox do EF Core no modo "bus outbox": publicar durante um caso de uso grava a mensagem em <c>outbox_message</c>
    /// na mesma transação dos dados, e um serviço em segundo plano entrega ao broker depois. Os testes usam este método com
    /// outro transporte.
    /// </summary>
    /// <param name="registro">Configurador do registro do MassTransit.</param>
    public static void ConfigurarOutbox(IBusRegistrationConfigurator registro)
    {
        ArgumentNullException.ThrowIfNull(registro);

        registro.SetKebabCaseEndpointNameFormatter();
        registro.AddEntityFrameworkOutbox<CampanhasDbContext>(outbox =>
        {
            outbox.UsePostgres();
            outbox.UseBusOutbox();
        });
    }

    /// <summary>
    /// Registra o MassTransit com o Outbox e o RabbitMQ (<c>RabbitMq:Host</c>, <c>RabbitMq:Usuario</c> e <c>RabbitMq:Senha</c>).
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">O host do RabbitMQ não foi configurado.</exception>
    public static IServiceCollection AddCampanhasMensageria(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var host = configuracao["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMq:Host não foi configurado.");
        var usuario = configuracao["RabbitMq:Usuario"] ?? "guest";
        var senha = configuracao["RabbitMq:Senha"] ?? "guest";

        servicos.AddMassTransit(registro =>
        {
            ConfigurarOutbox(registro);
            registro.UsingRabbitMq((contexto, bus) =>
            {
                bus.Host(host, "/", servidor =>
                {
                    servidor.Username(usuario);
                    servidor.Password(senha);
                });
                bus.ConfigureEndpoints(contexto);
            });
        });

        return servicos;
    }
}
