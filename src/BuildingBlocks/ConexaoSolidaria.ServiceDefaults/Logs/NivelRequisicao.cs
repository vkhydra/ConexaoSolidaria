using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace ConexaoSolidaria.ServiceDefaults.Logs;

/// <summary>
/// Define o nível de log de cada requisição HTTP: as sondas de saúde e de métricas não geram
/// ruído, e falhas do servidor sobem para erro.
/// </summary>
public static class NivelRequisicao
{
    /// <summary>
    /// Caminhos chamados com muita frequência por sondas e pelo Prometheus, que não devem ser logados.
    /// </summary>
    private static readonly string[] _caminhosSilenciosos = ["/health", "/metrics"];

    /// <summary>
    /// Calcula o nível de log de uma requisição concluída.
    /// </summary>
    /// <param name="contexto">Contexto HTTP da requisição.</param>
    /// <param name="duracaoMs">Duração da requisição, em milissegundos.</param>
    /// <param name="excecao">Exceção não tratada, se houve.</param>
    /// <returns>
    /// <see cref="LogEventLevel.Error"/> para exceção ou falha do servidor, <see cref="LogEventLevel.Verbose"/> para
    /// sondas (mesmo com 503) e <see cref="LogEventLevel.Information"/> nos demais casos.
    /// </returns>
    public static LogEventLevel Obter(HttpContext contexto, double duracaoMs, Exception? excecao)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (excecao is not null)
        {
            return LogEventLevel.Error;
        }

        foreach (var caminho in _caminhosSilenciosos)
        {
            if (contexto.Request.Path.StartsWithSegments(caminho, StringComparison.OrdinalIgnoreCase))
            {
                // Sonda de prontidão respondendo 503 durante a subida é esperado e não é erro.
                return LogEventLevel.Verbose;
            }
        }

        return contexto.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    }
}
