using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure.Leitura;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using Campanhas.IntegrationTests.Suporte;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Campanhas.IntegrationTests.Leitura;

/// <summary>
/// Testes da consulta do painel com Dapper contra um PostgreSQL real: filtro de status e de período, ordenação e valores.
/// </summary>
/// <param name="banco">Banco compartilhado pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class CampanhaLeituraDapperTests(BancoCampanhasFixture banco) : IClassFixture<BancoCampanhasFixture>
{
    /// <summary>
    /// Instante fixo da criação.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dadas campanhas ativas, concluídas, canceladas, vencidas e futuras, quando o painel é lido, então só as ativas no período aparecem, por data fim.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ListarAtivas_FiltraStatusEPeriodoEOrdenaPelaDataFim()
    {
        var hoje = new DateOnly(2027, 6, 15);
        var prefixo = Guid.NewGuid().ToString("N")[..8];
        await using (var escopo = banco.CriarEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
            await Gravar(repositorio, $"{prefixo} fim tarde", new DateOnly(2027, 6, 1), new DateOnly(2027, 9, 1));
            await Gravar(repositorio, $"{prefixo} fim cedo", new DateOnly(2027, 6, 1), new DateOnly(2027, 7, 1));
            await Gravar(repositorio, $"{prefixo} começa hoje", hoje, new DateOnly(2027, 8, 1));
            await Gravar(repositorio, $"{prefixo} termina hoje", new DateOnly(2027, 6, 1), hoje);
            await Gravar(repositorio, $"{prefixo} vencida", new DateOnly(2027, 5, 1), new DateOnly(2027, 6, 14));
            await Gravar(repositorio, $"{prefixo} futura", new DateOnly(2027, 6, 16), new DateOnly(2027, 8, 1));
            var concluida = await Gravar(repositorio, $"{prefixo} concluída", new DateOnly(2027, 6, 1), new DateOnly(2027, 9, 1));
            concluida.Concluir(_agora);
            await repositorio.SalvarAsync(concluida, CancellationToken.None);
            var cancelada = await Gravar(repositorio, $"{prefixo} cancelada", new DateOnly(2027, 6, 1), new DateOnly(2027, 9, 1));
            cancelada.Cancelar(_agora);
            await repositorio.SalvarAsync(cancelada, CancellationToken.None);
        }

        await using var fonte = NpgsqlDataSource.Create(banco.StringConexao);
        var painel = await new CampanhaLeituraDapper(fonte).ListarAtivasAsync(hoje, CancellationToken.None);

        var minhas = painel.Where(c => c.Titulo.StartsWith(prefixo, StringComparison.Ordinal)).ToList();
        minhas.Select(c => c.Titulo[(prefixo.Length + 1)..]).ShouldBe(["termina hoje", "fim cedo", "começa hoje", "fim tarde"]);
        minhas[0].MetaFinanceira.ShouldBe(5000m);
        minhas[0].TotalArrecadado.ShouldBe(0m);
        minhas[0].DataFim.ShouldBe(hoje);
    }

    /// <summary>
    /// Dado um dia sem campanhas ativas, quando o painel é lido, então a lista é vazia.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ListarAtivas_DiaSemCampanhas_DevolveListaVazia()
    {
        await using var fonte = NpgsqlDataSource.Create(banco.StringConexao);

        var painel = await new CampanhaLeituraDapper(fonte).ListarAtivasAsync(new DateOnly(2001, 1, 1), CancellationToken.None);

        painel.ShouldBeEmpty();
    }

    /// <summary>
    /// Grava uma campanha ativa de 5000.00.
    /// </summary>
    /// <param name="repositorio">Repositório de campanhas.</param>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="inicio">Data início.</param>
    /// <param name="fim">Data fim.</param>
    /// <returns>A campanha gravada.</returns>
    private static async Task<Campanha> Gravar(CampanhaRepositorio repositorio, string titulo, DateOnly inicio, DateOnly fim)
    {
        var campanha = Campanha.Criar(titulo, "d", Periodo.Criar(inicio, fim).Valor, Dinheiro.Criar(5000m).Valor, inicio < fim ? inicio : fim, _agora).Valor;
        await repositorio.AdicionarAsync(campanha, CancellationToken.None);
        return campanha;
    }
}
