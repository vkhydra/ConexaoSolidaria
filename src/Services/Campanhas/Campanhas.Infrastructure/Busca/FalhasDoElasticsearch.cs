using Campanhas.Application.Busca;
using Elastic.Transport;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Classifica as respostas de erro do Elasticsearch: indisponibilidade (rede, 429 e 5xx, que valem retentativa) ou erro de
/// uso da API (4xx, que não melhora ao repetir).
/// </summary>
internal static class FalhasDoElasticsearch
{
    /// <summary>
    /// Informa se o status indica que o Elasticsearch não conseguiu atender: sem resposta, sobrecarga (429) ou erro do servidor (5xx).
    /// </summary>
    /// <param name="status">Status HTTP da resposta; nulo quando nem houve resposta.</param>
    /// <returns>Verdadeiro se o problema é de disponibilidade.</returns>
    public static bool EhIndisponibilidade(int? status)
    {
        return status is null or 429 or >= 500;
    }

    /// <summary>
    /// Garante que a resposta foi bem-sucedida, convertendo a falha na exceção adequada.
    /// </summary>
    /// <param name="detalhes">Detalhes da chamada ao Elasticsearch (<c>ApiCallDetails</c> da resposta).</param>
    /// <param name="operacao">Descrição curta da operação, usada na mensagem (sem dados pessoais).</param>
    /// <exception cref="BuscaIndisponivelException">O Elasticsearch não respondeu ou respondeu 429 ou 5xx.</exception>
    /// <exception cref="InvalidOperationException">O Elasticsearch recusou a requisição (4xx).</exception>
    public static void ExigirSucesso(ApiCallDetails detalhes, string operacao)
    {
        ArgumentNullException.ThrowIfNull(detalhes);

        if (detalhes.HasSuccessfulStatusCode)
        {
            return;
        }

        var status = detalhes.HttpStatusCode;
        if (EhIndisponibilidade(status))
        {
            throw new BuscaIndisponivelException($"O Elasticsearch não atendeu a operação '{operacao}' (HTTP {status?.ToString() ?? "sem resposta"}).", detalhes.OriginalException ?? new HttpRequestException());
        }

        throw new InvalidOperationException($"O Elasticsearch recusou a operação '{operacao}' (HTTP {status}).");
    }
}
