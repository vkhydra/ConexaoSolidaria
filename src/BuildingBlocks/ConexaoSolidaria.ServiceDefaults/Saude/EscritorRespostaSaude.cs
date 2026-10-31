using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConexaoSolidaria.ServiceDefaults.Saude;

/// <summary>
/// Escreve o resultado dos health checks em JSON, com o estado de cada dependência.
/// Nunca inclui a exceção da verificação, porque ela pode conter cadeias de conexão e outros segredos.
/// </summary>
public static class EscritorRespostaSaude
{
    /// <summary>
    /// Opções de serialização: nomes em camelCase e enums como texto.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Escreve o relatório na resposta HTTP.
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição de saúde.</param>
    /// <param name="relatorio">Relatório gerado pelos health checks.</param>
    /// <returns>Tarefa que termina quando a resposta foi escrita.</returns>
    public static Task EscreverAsync(HttpContext contexto, HealthReport relatorio)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(relatorio);

        contexto.Response.ContentType = "application/json; charset=utf-8";
        var corpo = new RespostaSaude(
            relatorio.Status.ToString(),
            relatorio.TotalDuration.TotalMilliseconds,
            relatorio.Entries.ToDictionary(
                entrada => entrada.Key,
                entrada => new VerificacaoSaude(
                    entrada.Value.Status.ToString(),
                    DescricaoSegura(entrada.Value),
                    entrada.Value.Duration.TotalMilliseconds)));

        return JsonSerializer.SerializeAsync(contexto.Response.Body, corpo, _opcoes, contexto.RequestAborted);
    }

    /// <summary>
    /// Escolhe a descrição que pode ir na resposta. Quando a verificação lançou exceção, o ASP.NET Core usa a
    /// mensagem da exceção como descrição, e ela pode conter cadeia de conexão ou senha; nesse caso vai um texto fixo.
    /// </summary>
    /// <param name="entrada">Resultado da verificação.</param>
    /// <returns>Descrição segura para expor.</returns>
    private static string? DescricaoSegura(HealthReportEntry entrada)
    {
        return entrada.Exception is null ? entrada.Description : "A verificação da dependência falhou.";
    }

    /// <summary>
    /// Corpo da resposta de saúde.
    /// </summary>
    /// <param name="Status">Estado geral: <c>Healthy</c>, <c>Degraded</c> ou <c>Unhealthy</c>.</param>
    /// <param name="DuracaoMs">Tempo total das verificações, em milissegundos.</param>
    /// <param name="Verificacoes">Estado de cada dependência, pelo nome da verificação.</param>
    public sealed record RespostaSaude(string Status, double DuracaoMs, IReadOnlyDictionary<string, VerificacaoSaude> Verificacoes);

    /// <summary>
    /// Resultado de uma verificação de dependência.
    /// </summary>
    /// <param name="Status">Estado da dependência.</param>
    /// <param name="Descricao">Descrição curta definida pela verificação, sem detalhes sensíveis.</param>
    /// <param name="DuracaoMs">Tempo da verificação, em milissegundos.</param>
    public sealed record VerificacaoSaude(string Status, string? Descricao, double DuracaoMs);
}
