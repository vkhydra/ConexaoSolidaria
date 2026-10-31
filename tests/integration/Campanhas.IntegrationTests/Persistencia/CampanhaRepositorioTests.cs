using System.Text.Json;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure.Persistencia;
using Campanhas.Infrastructure.Persistencia.Repositorios;
using Campanhas.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.IntegrationTests.Persistencia;

/// <summary>
/// Testes do repositório de campanhas contra um PostgreSQL real: gravação, Outbox transacional, concorrência por xmin e listagem.
/// </summary>
/// <param name="banco">Banco compartilhado pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class CampanhaRepositorioTests(BancoCampanhasFixture banco) : IClassFixture<BancoCampanhasFixture>
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Instante fixo da criação, em São Paulo.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dada uma campanha nova, quando gravada, então volta igual do banco e o evento fica no Outbox com o estado atual.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Adicionar_CampanhaNova_GravaDadosEEventoNoOutbox()
    {
        var campanha = NovaCampanha("Natal Solidário", new DateOnly(2026, 12, 24));
        await using (var escopo = banco.CriarEscopo())
        {
            await escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().AdicionarAsync(campanha, CancellationToken.None);
        }

        await using var leitura = banco.CriarEscopo();
        var lida = await leitura.ServiceProvider.GetRequiredService<CampanhaRepositorio>().ObterPorIdAsync(campanha.Id, CancellationToken.None);

        lida.ShouldNotBeNull();
        lida.Titulo.ShouldBe("Natal Solidário");
        lida.Periodo.DataFim.ShouldBe(new DateOnly(2026, 12, 24));
        lida.MetaFinanceira.Valor.ShouldBe(10000m);
        lida.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        lida.Status.ShouldBe(StatusCampanha.Ativa);
        lida.CriadoEm.UtcDateTime.ShouldBe(_agora.UtcDateTime);
        lida.Versao.ShouldBeGreaterThan(0u);
        campanha.Eventos.ShouldBeEmpty();

        var outbox = await banco.LerOutboxAsync(campanha.Id.Valor);
        var corpo = JsonDocument.Parse(outbox.ShouldHaveSingleItem()).RootElement.GetProperty("message");
        corpo.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id.Valor);
        corpo.GetProperty("titulo").GetString().ShouldBe("Natal Solidário");
        corpo.GetProperty("status").GetString().ShouldBe("Ativa");
        corpo.GetProperty("dataFim").GetString().ShouldBe("2026-12-24");
        corpo.GetProperty("versao").GetInt64().ShouldBe(_agora.UtcTicks);
    }

    /// <summary>
    /// Dada uma gravação que falha, quando o banco recusa a campanha, então o evento também não fica no Outbox (mesma transação).
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Adicionar_GravacaoFalha_NaoDeixaEventoNoOutbox()
    {
        var campanha = NovaCampanha("Falha", new DateOnly(2026, 12, 24));
        await using (var escopo = banco.CriarEscopo())
        {
            await escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().AdicionarAsync(campanha, CancellationToken.None);
        }

        var repetida = NovaCampanha("Repetida", new DateOnly(2026, 12, 24));
        typeof(Campanha).GetProperty(nameof(Campanha.Id))!.SetValue(repetida, campanha.Id);
        await using (var escopo = banco.CriarEscopo())
        {
            await Should.ThrowAsync<DbUpdateException>(() => escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().AdicionarAsync(repetida, CancellationToken.None));
        }

        (await banco.LerOutboxAsync(campanha.Id.Valor)).Count.ShouldBe(1);
    }

    /// <summary>
    /// Dada uma campanha gravada, quando é alterada e salva, então o banco guarda a mudança, a versão (xmin) muda e um segundo evento entra no Outbox.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Salvar_CampanhaAlterada_GravaMudancaEMaisUmEvento()
    {
        var campanha = NovaCampanha("Para editar", new DateOnly(2026, 12, 24));
        await using (var escopo = banco.CriarEscopo())
        {
            await escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().AdicionarAsync(campanha, CancellationToken.None);
        }

        uint versaoAntes;
        await using (var escopo = banco.CriarEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
            var carregada = (await repositorio.ObterPorIdAsync(campanha.Id, CancellationToken.None))!;
            versaoAntes = carregada.Versao;
            carregada.Concluir(_agora.AddHours(1));
            await repositorio.SalvarAsync(carregada, CancellationToken.None);
        }

        await using var leitura = banco.CriarEscopo();
        var lida = (await leitura.ServiceProvider.GetRequiredService<CampanhaRepositorio>().ObterPorIdAsync(campanha.Id, CancellationToken.None))!;
        lida.Status.ShouldBe(StatusCampanha.Concluida);
        lida.Versao.ShouldNotBe(versaoAntes);
        var outbox = await banco.LerOutboxAsync(campanha.Id.Valor);
        outbox.Count.ShouldBe(2);
        JsonDocument.Parse(outbox[1]).RootElement.GetProperty("message").GetProperty("status").GetString().ShouldBe("Concluida");
    }

    /// <summary>
    /// Dadas duas edições simultâneas da mesma versão, quando a segunda é salva, então o repositório sinaliza conflito de versão e não deixa evento extra.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Salvar_EdicaoConcorrente_LancaConflitoDeVersao()
    {
        var campanha = NovaCampanha("Concorrente", new DateOnly(2026, 12, 24));
        await using (var escopo = banco.CriarEscopo())
        {
            await escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().AdicionarAsync(campanha, CancellationToken.None);
        }

        await using var a = banco.CriarEscopo();
        await using var b = banco.CriarEscopo();
        var repositorioA = a.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
        var repositorioB = b.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
        var campanhaA = (await repositorioA.ObterPorIdAsync(campanha.Id, CancellationToken.None))!;
        var campanhaB = (await repositorioB.ObterPorIdAsync(campanha.Id, CancellationToken.None))!;
        campanhaA.Cancelar(_agora.AddHours(1));
        campanhaB.Concluir(_agora.AddHours(2));

        await repositorioA.SalvarAsync(campanhaA, CancellationToken.None);

        await Should.ThrowAsync<ConflitoDeVersaoException>(() => repositorioB.SalvarAsync(campanhaB, CancellationToken.None));
        (await banco.LerOutboxAsync(campanha.Id.Valor)).Count.ShouldBe(2);
    }

    /// <summary>
    /// Dadas campanhas de datas fim diferentes, quando listadas com filtro e paginação, então vêm ordenadas pela data fim, filtradas e com o total certo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Listar_FiltroEPaginacao_OrdenaPelaDataFim()
    {
        var prefixo = Guid.NewGuid().ToString("N")[..8];
        var datas = new[] { new DateOnly(2027, 3, 1), new DateOnly(2027, 1, 1), new DateOnly(2027, 2, 1) };
        await using (var escopo = banco.CriarEscopo())
        {
            var repositorio = escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
            foreach (var data in datas)
            {
                await repositorio.AdicionarAsync(NovaCampanha($"{prefixo} {data:yyyy-MM}", data), CancellationToken.None);
            }

            var cancelada = NovaCampanha($"{prefixo} cancelada", new DateOnly(2027, 1, 15));
            await repositorio.AdicionarAsync(cancelada, CancellationToken.None);
            cancelada.Cancelar(_agora);
            await repositorio.SalvarAsync(cancelada, CancellationToken.None);
        }

        await using var leitura = banco.CriarEscopo();
        var consulta = leitura.ServiceProvider.GetRequiredService<CampanhasDbContext>().Campanhas.Where(c => c.Titulo.StartsWith(prefixo)).Select(c => c.Id);
        var ids = await consulta.ToListAsync(TestContext.Current.CancellationToken);
        ids.Count.ShouldBe(4);

        var repo = leitura.ServiceProvider.GetRequiredService<CampanhaRepositorio>();
        var ativas = await repo.ListarAsync(StatusCampanha.Ativa, 1, 100, CancellationToken.None);
        var minhasAtivas = ativas.Itens.Where(c => c.Titulo.StartsWith(prefixo, StringComparison.Ordinal)).ToList();
        minhasAtivas.Select(c => c.Periodo.DataFim).ShouldBe([new DateOnly(2027, 1, 1), new DateOnly(2027, 2, 1), new DateOnly(2027, 3, 1)]);

        var todas = await repo.ListarAsync(null, 1, 2, CancellationToken.None);
        todas.Itens.Count.ShouldBe(2);
        todas.Total.ShouldBeGreaterThanOrEqualTo(4);
        todas.Numero.ShouldBe(1);
        todas.TamanhoPagina.ShouldBe(2);
    }

    /// <summary>
    /// Dado um identificador inexistente, quando buscado, então devolve nulo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterPorId_Inexistente_DevolveNulo()
    {
        await using var escopo = banco.CriarEscopo();

        (await escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>().ObterPorIdAsync(CampanhaId.Novo(), CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>
    /// Dados argumentos nulos, quando o repositório é chamado, então recusa.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Repositorio_ArgumentosNulos_LancaExcecao()
    {
        await using var escopo = banco.CriarEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<CampanhaRepositorio>();

        await Should.ThrowAsync<ArgumentNullException>(() => repositorio.AdicionarAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => repositorio.SalvarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Cria uma campanha ativa de 10000.00 para os testes.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="fim">Data fim.</param>
    /// <returns>A campanha criada, ainda não gravada.</returns>
    private static Campanha NovaCampanha(string titulo, DateOnly fim)
    {
        return Campanha.Criar(titulo, "Descrição", Periodo.Criar(_hoje, fim).Valor, Dinheiro.Criar(10000m).Valor, _hoje, _agora).Valor;
    }
}
