using System.Text.Json;
using System.Text.Json.Nodes;
using ConexaoSolidaria.Contracts.V1;

namespace ConexaoSolidaria.ContractTests;

/// <summary>
/// Teste de contrato do <see cref="CampanhaAlteradaEvent"/> v1: o JSON publicado precisa continuar igual ao snapshot
/// versionado. Mudar o contrato exige criar a versão V2.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CampanhaAlteradaEventTests
{
    /// <summary>
    /// Opções de serialização iguais às do MassTransit: camelCase e sem formatação.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Dado o evento do exemplo do contrato, quando serializado, então o JSON é igual ao snapshot.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Serializar_EventoDoContrato_CorrespondeAoSnapshot()
    {
        var evento = new CampanhaAlteradaEvent(
            Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f"),
            "Natal Solidário",
            "Presentes e ceia para 120 crianças.",
            "Ativa",
            10000.00m,
            new DateOnly(2026, 12, 24),
            1);
        var snapshot = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Snapshots", "CampanhaAlteradaEvent.v1.json"), TestContext.Current.CancellationToken);

        var gerado = JsonNode.Parse(JsonSerializer.Serialize(evento, _opcoes));

        JsonNode.DeepEquals(gerado, JsonNode.Parse(snapshot)).ShouldBeTrue($"O JSON gerado mudou: {gerado}");
    }

    /// <summary>
    /// Dado o snapshot versionado, quando desserializado, então reconstrói o evento sem perder campos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Desserializar_Snapshot_ReconstroiOEvento()
    {
        var snapshot = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Snapshots", "CampanhaAlteradaEvent.v1.json"), TestContext.Current.CancellationToken);

        var evento = JsonSerializer.Deserialize<CampanhaAlteradaEvent>(snapshot, _opcoes);

        evento.ShouldNotBeNull();
        evento.CampanhaId.ShouldBe(Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f"));
        evento.Status.ShouldBe("Ativa");
        evento.MetaFinanceira.ShouldBe(10000.00m);
        evento.DataFim.ShouldBe(new DateOnly(2026, 12, 24));
        evento.Versao.ShouldBe(1);
    }

    /// <summary>
    /// Dado o contrato v1, quando os campos são listados, então são exatamente os do snapshot e na mesma ordem: só se acrescenta no fim.
    /// </summary>
    [Fact]
    public void Campos_ContratoV1_SaoOsDoSnapshotNaMesmaOrdem()
    {
        var campos = typeof(CampanhaAlteradaEvent).GetConstructors().Single().GetParameters().Select(p => p.Name).ToArray();

        campos.ShouldBe(["CampanhaId", "Titulo", "Descricao", "Status", "MetaFinanceira", "DataFim", "Versao"]);
    }
}
