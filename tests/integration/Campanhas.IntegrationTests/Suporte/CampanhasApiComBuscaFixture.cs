namespace Campanhas.IntegrationTests.Suporte;

/// <summary>
/// A Campanhas.Api de teste ligada ao Elasticsearch real do container compartilhado da coleção <c>Elasticsearch</c>.
/// </summary>
/// <param name="elasticsearch">Elasticsearch compartilhado pela coleção.</param>
public sealed class CampanhasApiComBuscaFixture(ElasticsearchFixture elasticsearch) : CampanhasApiFixture
{
    /// <inheritdoc/>
    protected override IReadOnlyDictionary<string, string> ConfiguracaoElasticsearch => elasticsearch.ConfiguracaoDaApi;
}
