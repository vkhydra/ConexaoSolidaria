using System.Text.Json;
using System.Text.Json.Nodes;
using ConexaoSolidaria.Contracts.V1;

namespace ConexaoSolidaria.ContractTests;

/// <summary>
/// Testes de contrato dos eventos de doação v1: o JSON publicado precisa continuar igual ao snapshot versionado.
/// Mudar o contrato exige criar a versão V2.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DoacaoEventosTests
{
    /// <summary>
    /// Opções de serialização iguais às do MassTransit: camelCase e sem formatação.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Identificador da doação do exemplo.
    /// </summary>
    private static readonly Guid _doacao = Guid.Parse("0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f");

    /// <summary>
    /// Identificador da campanha do exemplo.
    /// </summary>
    private static readonly Guid _campanha = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");

    /// <summary>
    /// Identificador do doador do exemplo.
    /// </summary>
    private static readonly Guid _doador = Guid.Parse("0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70");

    /// <summary>
    /// Instante em que a doação foi aceita, no exemplo.
    /// </summary>
    private static readonly DateTimeOffset _recebida = new(2026, 11, 10, 14, 10, 5, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dado o evento de doação recebida do exemplo do contrato, quando serializado, então o JSON é igual ao snapshot.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Serializar_DoacaoRecebida_CorrespondeAoSnapshot()
    {
        var evento = new DoacaoRecebidaEvent(_doacao, _campanha, _doador, 150.00m, _recebida);

        var gerado = JsonNode.Parse(JsonSerializer.Serialize(evento, _opcoes));

        JsonNode.DeepEquals(gerado, JsonNode.Parse(await LerSnapshotAsync("DoacaoRecebidaEvent"))).ShouldBeTrue($"O JSON gerado mudou: {gerado}");
    }

    /// <summary>
    /// Dado o evento de doação processada do exemplo do contrato, quando serializado, então o JSON é igual ao snapshot.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Serializar_DoacaoProcessada_CorrespondeAoSnapshot()
    {
        var evento = new DoacaoProcessadaEvent(_doacao, _campanha, _doador, 150.00m, 1150.00m, _recebida, _recebida.AddSeconds(1));

        var gerado = JsonNode.Parse(JsonSerializer.Serialize(evento, _opcoes));

        JsonNode.DeepEquals(gerado, JsonNode.Parse(await LerSnapshotAsync("DoacaoProcessadaEvent"))).ShouldBeTrue($"O JSON gerado mudou: {gerado}");
    }

    /// <summary>
    /// Dados os snapshots versionados, quando desserializados, então reconstroem os eventos sem perder campos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Desserializar_Snapshots_ReconstroemOsEventos()
    {
        var recebida = JsonSerializer.Deserialize<DoacaoRecebidaEvent>(await LerSnapshotAsync("DoacaoRecebidaEvent"), _opcoes);
        var processada = JsonSerializer.Deserialize<DoacaoProcessadaEvent>(await LerSnapshotAsync("DoacaoProcessadaEvent"), _opcoes);

        recebida.ShouldBe(new DoacaoRecebidaEvent(_doacao, _campanha, _doador, 150.00m, _recebida));
        processada.ShouldBe(new DoacaoProcessadaEvent(_doacao, _campanha, _doador, 150.00m, 1150.00m, _recebida, _recebida.AddSeconds(1)));
    }

    /// <summary>
    /// Dado o contrato v1, quando os campos são listados, então são exatamente os do snapshot e na mesma ordem: só se acrescenta no fim.
    /// </summary>
    [Fact]
    public void Campos_ContratoV1_SaoOsDoSnapshotNaMesmaOrdem()
    {
        NomesDosCampos(typeof(DoacaoRecebidaEvent)).ShouldBe(["DoacaoId", "CampanhaId", "DoadorId", "Valor", "OcorridoEm"]);
        NomesDosCampos(typeof(DoacaoProcessadaEvent)).ShouldBe(["DoacaoId", "CampanhaId", "DoadorId", "Valor", "TotalArrecadado", "RecebidaEm", "ProcessadaEm"]);
    }

    /// <summary>
    /// Lista os nomes dos parâmetros do construtor do record, na ordem declarada.
    /// </summary>
    /// <param name="tipo">Tipo do evento.</param>
    /// <returns>Os nomes dos campos.</returns>
    private static string?[] NomesDosCampos(Type tipo)
    {
        return tipo.GetConstructors().Single().GetParameters().Select(p => p.Name).ToArray();
    }

    /// <summary>
    /// Lê o snapshot versionado de um evento.
    /// </summary>
    /// <param name="evento">Nome do evento, sem a extensão.</param>
    /// <returns>O texto do JSON.</returns>
    private static Task<string> LerSnapshotAsync(string evento)
    {
        return File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Snapshots", $"{evento}.v1.json"), TestContext.Current.CancellationToken);
    }
}
