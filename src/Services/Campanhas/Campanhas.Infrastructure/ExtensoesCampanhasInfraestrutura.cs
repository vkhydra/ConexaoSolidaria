using Campanhas.Application.Abstracoes;
using Campanhas.Application.Arrecadacao;
using Campanhas.Application.Busca;
using Campanhas.Application.Doacoes;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure.Arrecadacao;
using Campanhas.Infrastructure.Busca;
using Campanhas.Infrastructure.Cache;
using Campanhas.Infrastructure.Idempotencia;
using Campanhas.Infrastructure.Leitura;
using Campanhas.Infrastructure.Mensageria;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using MassTransit;
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
        servicos.AddSingleton<IMetricasArrecadacao, MetricasDoacoesPrometheus>();
        servicos.AddScoped<IRegistroArrecadacao, RegistroArrecadacaoSql>();
        return servicos;
    }

    /// <summary>
    /// Registra a persistência, o repositório, a consulta do painel (Dapper), as doações com idempotência, o cache e o
    /// MassTransit com Outbox e RabbitMQ: tudo o que a Campanhas.Api usa.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <param name="configurarMensageria">Registro extra do MassTransit, por exemplo dos consumidores da API; nulo quando não há.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A string de conexão, o host do RabbitMQ ou o endereço do Elasticsearch não foi configurado.</exception>
    public static IServiceCollection AddCampanhasInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao, Action<IBusRegistrationConfigurator>? configurarMensageria = null)
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
        servicos.AddSingleton<IMetricasDoacoes, MetricasDoacoesPrometheus>();
        servicos.AddCampanhasCache(configuracao);
        servicos.AddCampanhasBusca(configuracao);
        servicos.AddCampanhasMensageria(configuracao, configurarMensageria);

        return servicos;
    }

    /// <summary>
    /// Registra o cliente do Elasticsearch (<c>Elasticsearch:Endereco</c>, com <c>Elasticsearch:ApiKey</c> ou
    /// <c>Elasticsearch:Usuario</c> e <c>Elasticsearch:Senha</c> quando há autenticação, e <c>Elasticsearch:ImpressaoDigital</c>,
    /// o SHA-256 do certificado, quando o servidor usa certificado autoassinado) e as portas de busca, de indexação e de reindexação.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">O endereço do Elasticsearch não foi configurado.</exception>
    public static IServiceCollection AddCampanhasBusca(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var endereco = configuracao["Elasticsearch:Endereco"]
            ?? throw new InvalidOperationException("Elasticsearch:Endereco não foi configurado.");
        var apiKey = configuracao["Elasticsearch:ApiKey"];
        var usuario = configuracao["Elasticsearch:Usuario"];
        var senha = configuracao["Elasticsearch:Senha"];
        var impressaoDigital = configuracao["Elasticsearch:ImpressaoDigital"];

        var opcoes = new ElasticsearchClientSettings(new Uri(endereco)).RequestTimeout(TimeSpan.FromSeconds(5));
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            opcoes.Authentication(new ApiKey(apiKey));
        }
        else if (!string.IsNullOrWhiteSpace(usuario))
        {
            opcoes.Authentication(new BasicAuthentication(usuario, senha ?? string.Empty));
        }

        if (!string.IsNullOrWhiteSpace(impressaoDigital))
        {
            opcoes.CertificateFingerprint(impressaoDigital.Replace(":", string.Empty, StringComparison.Ordinal));
        }

        servicos.AddSingleton(new ElasticsearchClient(opcoes));
        servicos.AddSingleton<IIndiceCampanhas, IndiceCampanhasElasticsearch>();
        servicos.AddSingleton<IBuscaCampanhas, BuscaCampanhasElasticsearch>();
        servicos.AddSingleton<IMetricasBusca, MetricasBuscaPrometheus>();
        servicos.AddSingleton<IGerenciadorDeIndice, GerenciadorDeIndiceElasticsearch>();
        servicos.AddSingleton<IFonteDeCampanhas, FonteDeCampanhasDapper>();
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
