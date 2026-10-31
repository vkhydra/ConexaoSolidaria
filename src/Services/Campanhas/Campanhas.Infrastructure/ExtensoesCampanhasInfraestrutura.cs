using Campanhas.Application.Abstracoes;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure.Leitura;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Campanhas.Infrastructure;

/// <summary>
/// Registro dos serviços de infraestrutura de Campanhas (banco, repositório, relógio e mensageria).
/// </summary>
public static class ExtensoesCampanhasInfraestrutura
{
    /// <summary>
    /// Registra o <see cref="CampanhasDbContext"/> (<c>ConnectionStrings:Campanhas</c>), o repositório, a consulta do painel (Dapper), o relógio de São Paulo
    /// e o MassTransit com Outbox e RabbitMQ.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A string de conexão ou o host do RabbitMQ não foi configurado.</exception>
    public static IServiceCollection AddCampanhasInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var conexao = configuracao.GetConnectionString("Campanhas")
            ?? throw new InvalidOperationException("ConnectionStrings:Campanhas não foi configurada.");

        servicos.AddDbContext<CampanhasDbContext>(opcoes => CampanhasDbContext.Configurar(opcoes, conexao));
        servicos.AddScoped<ICampanhaRepositorio, CampanhaRepositorio>();
        servicos.AddSingleton(NpgsqlDataSource.Create(conexao));
        servicos.AddSingleton<ICampanhaLeitura, CampanhaLeituraDapper>();
        servicos.AddSingleton<IRelogio, RelogioSaoPaulo>();
        servicos.AddSingleton<IMetricasCampanhas, MetricasCampanhasPrometheus>();
        servicos.AddCampanhasMensageria(configuracao);

        return servicos;
    }
}
