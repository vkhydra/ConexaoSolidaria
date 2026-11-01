namespace Campanhas.IntegrationTests.Suporte;

/// <summary>
/// Agrupa as classes de teste que usam o Elasticsearch para que dividam um único container (ele consome muita memória) e
/// rodem uma de cada vez.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoElasticsearch : ICollectionFixture<ElasticsearchFixture>
{
    /// <summary>
    /// Nome da coleção, citado por <c>[Collection]</c> nas classes de teste.
    /// </summary>
    public const string Nome = "Elasticsearch";
}
