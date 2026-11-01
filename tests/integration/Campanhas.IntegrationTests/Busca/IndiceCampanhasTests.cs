using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Busca;
using Campanhas.IntegrationTests.Suporte;
using Elastic.Clients.Elasticsearch;

namespace Campanhas.IntegrationTests.Busca;

/// <summary>
/// Testes da gravação de campanhas no índice do Elasticsearch: versão externa, substituição e falhas.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ColecaoElasticsearch.Nome)]
public sealed class IndiceCampanhasTests(ElasticsearchFixture elasticsearch)
{
    /// <summary>
    /// Dado um documento novo, quando gravado, então é indexado e fica legível no alias.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Gravar_DocumentoNovo_Indexa()
    {
        var ct = TestContext.Current.CancellationToken;
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);
        var campanha = Novo("Natal Solidário", versao: 10);

        var resultado = await indice.GravarAsync(campanha, ct);

        resultado.ShouldBe(ResultadoIndexacao.Indexada);
        var lido = await elasticsearch.Cliente.GetAsync<IndiceCampanhasElasticsearch.DocumentoCampanha>(MapeamentoIndice.NomeAlias, campanha.Id.ToString(), ct);
        lido.Found.ShouldBeTrue();
        lido.Source!.Titulo.ShouldBe("Natal Solidário");
        lido.Source.DataFim.ShouldBe(new DateOnly(2026, 12, 24));
        lido.Source.MetaFinanceira.ShouldBe(5000.00m);
    }

    /// <summary>
    /// Dado um documento já gravado, quando chega uma versão mais nova, então ela substitui a anterior.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Gravar_VersaoMaisNova_Substitui()
    {
        var ct = TestContext.Current.CancellationToken;
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);
        var id = Guid.NewGuid();
        await indice.GravarAsync(Novo("Antigo", versao: 1, id), ct);

        var resultado = await indice.GravarAsync(Novo("Novo", versao: 2, id), ct);

        resultado.ShouldBe(ResultadoIndexacao.Indexada);
        var lido = await elasticsearch.Cliente.GetAsync<IndiceCampanhasElasticsearch.DocumentoCampanha>(MapeamentoIndice.NomeAlias, id.ToString(), ct);
        lido.Source!.Titulo.ShouldBe("Novo");
    }

    /// <summary>
    /// Dado um documento já gravado, quando chega uma versão igual ou mais antiga (evento fora de ordem ou repetido), então é ignorada.
    /// </summary>
    /// <param name="versao">Versão que chega depois da versão 5.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData(5)]
    [InlineData(4)]
    public async Task Gravar_VersaoIgualOuAntiga_Ignora(long versao)
    {
        var ct = TestContext.Current.CancellationToken;
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);
        var id = Guid.NewGuid();
        await indice.GravarAsync(Novo("Mais nova", versao: 5, id), ct);

        var resultado = await indice.GravarAsync(Novo("Atrasada", versao, id), ct);

        resultado.ShouldBe(ResultadoIndexacao.VersaoAntiga);
        var lido = await elasticsearch.Cliente.GetAsync<IndiceCampanhasElasticsearch.DocumentoCampanha>(MapeamentoIndice.NomeAlias, id.ToString(), ct);
        lido.Source!.Titulo.ShouldBe("Mais nova");
    }

    /// <summary>
    /// Dado um Elasticsearch inalcançável, quando se grava, então a falha vira <see cref="BuscaIndisponivelException"/>.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Gravar_ElasticsearchInalcancavel_LancaIndisponivel()
    {
        var cliente = new ElasticsearchClient(new Uri("http://127.0.0.1:1"));
        var indice = new IndiceCampanhasElasticsearch(cliente);

        await Should.ThrowAsync<BuscaIndisponivelException>(() => indice.GravarAsync(Novo("Qualquer", versao: 1), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dado um documento nulo, quando se grava, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Gravar_DocumentoNulo_LancaExcecao()
    {
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);

        await Should.ThrowAsync<ArgumentNullException>(() => indice.GravarAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Dado um documento que o Elasticsearch recusa (data fora do mapeamento), quando gravado, então a falha de uso é sinalizada e não confundida com indisponibilidade.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Gravar_DocumentoRecusado_LancaOperacaoInvalida()
    {
        var indice = new IndiceCampanhasElasticsearch(elasticsearch.Cliente);
        var invalido = Novo("Sem versão válida", versao: -1);

        await Should.ThrowAsync<InvalidOperationException>(() => indice.GravarAsync(invalido, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Cria um documento de campanha ativa.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="versao">Versão do documento.</param>
    /// <param name="id">Identificador; sorteado quando não informado.</param>
    /// <returns>O documento.</returns>
    private static CampanhaIndexada Novo(string titulo, long versao, Guid? id = null)
    {
        return new CampanhaIndexada(id ?? Guid.NewGuid(), titulo, "Descrição da campanha", "Ativa", 5000.00m, new DateOnly(2026, 12, 24), versao);
    }
}
