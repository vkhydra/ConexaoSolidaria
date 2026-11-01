using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Campanhas.Api.Saude;

/// <summary>
/// Verifica se o Elasticsearch responde. Só a busca depende dele, então a falha é <c>Degraded</c> e não
/// derruba a prontidão da API. A descrição é um texto fixo, sem a exceção (que poderia trazer o endereço ou credenciais).
/// </summary>
/// <param name="cliente">Cliente do Elasticsearch.</param>
public sealed class ElasticsearchHealthCheck(ElasticsearchClient cliente) : IHealthCheck
{
    /// <summary>
    /// Nome do registro da verificação, que aparece no relatório de <c>/health/ready</c>.
    /// </summary>
    public const string Nome = "elasticsearch";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var resposta = await cliente.PingAsync(cancellationToken);
        return resposta.IsValidResponse
            ? HealthCheckResult.Healthy("Elasticsearch responde.")
            : HealthCheckResult.Degraded("Elasticsearch não responde; a busca está indisponível.");
    }
}
