using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia;
using Identity.Infrastructure.Persistencia.Repositorios;
using Identity.Infrastructure.Seed;
using Identity.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Registro dos serviços de infraestrutura da Identity (banco, repositórios, hash e token).
/// </summary>
public static class ExtensoesIdentityInfraestrutura
{
    /// <summary>
    /// Registra o <see cref="IdentityDbContext"/> (<c>ConnectionStrings:Identity</c>), os repositórios, o hash de senha,
    /// o hash HMAC do e-mail (<c>Autenticacao:ChaveHashEmail</c>), a proteção do CPF (seção <c>Cpf</c>), o emissor de JWT (seção <c>Jwt</c>) e o seed do gestor inicial.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">A string de conexão ou a chave do HMAC não foi configurada.</exception>
    /// <exception cref="ArgumentException">As chaves do CPF estão ausentes ou inválidas.</exception>
    public static IServiceCollection AddIdentityInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        var conexao = configuracao.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("ConnectionStrings:Identity não foi configurada.");
        var chaveHashEmail = configuracao["Autenticacao:ChaveHashEmail"]
            ?? throw new InvalidOperationException("Autenticacao:ChaveHashEmail não foi configurada.");

        servicos.AddDbContext<IdentityDbContext>(opcoes => opcoes.UseNpgsql(conexao));
        servicos.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicos.AddScoped<ITentativaLoginRepositorio, TentativaLoginRepositorio>();
        servicos.AddSingleton<IHasherSenha, HasherSenhaBcrypt>();
        servicos.AddSingleton<IMetricasLogin, MetricasLoginPrometheus>();
        servicos.AddSingleton<IMetricasDoadores, MetricasDoadoresPrometheus>();
        servicos.AddSingleton<IHasherEmail>(new HasherEmailHmac(chaveHashEmail));
        servicos.AddSingleton<ICifradorCpf>(new CifradorCpfAesGcm(new CpfOpcoes
        {
            ChaveCifra = configuracao["Cpf:ChaveCifra"] ?? string.Empty,
            ChaveHash = configuracao["Cpf:ChaveHash"] ?? string.Empty,
            VersaoChave = byte.TryParse(configuracao["Cpf:VersaoChave"], out var versao) ? versao : (byte)1,
        }));

        var jwt = new JwtOpcoes
        {
            Issuer = configuracao["Jwt:Issuer"] ?? "conexao-identity",
            Audience = configuracao["Jwt:Audience"] ?? "conexao-api",
            ChavePrivadaPem = configuracao["Jwt:ChavePrivadaPem"] ?? string.Empty,
        };
        servicos.AddSingleton<IEmissorToken>(provedor => new EmissorJwtRs256(jwt, provedor.GetRequiredService<TimeProvider>()));

        servicos.AddHostedService<SeedGestorHostedService>();

        return servicos;
    }
}
