using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Doacoes;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure.Arrecadacao;
using Campanhas.Infrastructure.Cache;
using Campanhas.Infrastructure.Idempotencia;
using Campanhas.Infrastructure.Leitura;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Npgsql;

namespace Campanhas.Infrastructure;

/// <summary>
/// Registro dos serviços de infraestrutura de Campanhas (banco, repositório, relógio e mensageria).
/// </summary>
public static class ExtensoesCampanhasInfraestrutura
{
    /// <summary>
    /// Registra o <see cref="CampanhasDbContext"/> (<c>ConnectionStrings:Campanhas</c>), a fonte de dados do Npgsql e o relógio de São Paulo.
    /// É a base comum da Campanhas.Api e do Arrecadacao.Worker.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A string de conexão não foi configurada.</exception>
    public static IServiceCollection AddCampanhasPersistencia(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var conexao = configuracao.GetConnectionString("Campanhas")
            ?? throw new InvalidOperationException("ConnectionStrings:Campanhas não foi configurada.");

        servicos.AddDbContext<CampanhasDbContext>(opcoes => CampanhasDbContext.Configurar(opcoes, conexao));
        servicos.AddSingleton(NpgsqlDataSource.Create(conexao));
        servicos.AddSingleton<IRelogio, RelogioSaoPaulo>();
        return servicos;
    }

    /// <summary>
    /// Registra o registro de arrecadação (PostgreSQL) e o livro de doações (MongoDB, <c>ConnectionStrings:Mongo</c>) do Arrecadacao.Worker.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A conexão do MongoDB não foi configurada.</exception>
    public static IServiceCollection AddCampanhasArrecadacao(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var mongo = configuracao.GetConnectionString("Mongo")
            ?? throw new InvalidOperationException("ConnectionStrings:Mongo não foi configurada.");

        servicos.AddSingleton<IMongoClient>(new MongoClient(mongo));
        servicos.AddSingleton(provedor => provedor.GetRequiredService<IMongoClient>().GetDatabase(LivroDoacoesMongo.NomeBanco));
        servicos.AddSingleton<ILivroDoacoes, LivroDoacoesMongo>();
        servicos.AddScoped<IRegistroArrecadacao, RegistroArrecadacaoSql>();
        return servicos;
    }

    /// <summary>
    /// Registra a persistência, o repositório, a consulta do painel (Dapper), as doações com idempotência, o cache e o
    /// MassTransit com Outbox e RabbitMQ: tudo o que a Campanhas.Api usa.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A string de conexão ou o host do RabbitMQ não foi configurado.</exception>
    public static IServiceCollection AddCampanhasInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        servicos.AddCampanhasPersistencia(configuracao);
        servicos.AddScoped<ICampanhaRepositorio, CampanhaRepositorio>();
        servicos.AddScoped<IPublicadorDeDoacoes, PublicadorDeDoacoes>();
        servicos.AddScoped<IChavesIdempotencia, ChavesIdempotenciaRepositorio>();
        servicos.AddHostedService<LimpezaChavesIdempotenciaHostedService>();
        servicos.AddSingleton<ICampanhaLeitura, CampanhaLeituraDapper>();
        servicos.AddSingleton<IMetricasCampanhas, MetricasCampanhasPrometheus>();
        servicos.AddCampanhasCache(configuracao);
        servicos.AddCampanhasMensageria(configuracao);

        return servicos;
    }

    /// <summary>
    /// Registra o HybridCache com o Redis como cache distribuído (<c>ConnectionStrings:Redis</c>) e o cache do painel.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A conexão do Redis não foi configurada.</exception>
    public static IServiceCollection AddCampanhasCache(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var redis = configuracao.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis não foi configurada.");

        servicos.AddStackExchangeRedisCache(opcoes =>
        {
            opcoes.Configuration = redis;
            opcoes.InstanceName = "campanhas:";
        });
        servicos.AddHybridCache();
        servicos.AddSingleton<IMetricasPainel, MetricasPainelPrometheus>();
        servicos.AddSingleton<ICachePainel, CachePainelHybrid>();
        return servicos;
    }
}
