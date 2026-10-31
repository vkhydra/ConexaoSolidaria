using Campanhas.Application.Abstracoes;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Transparencia;

/// <summary>
/// Caso de uso do painel público: campanhas ativas no período, com meta, total e percentual atingido. Usa o cache quando
/// ele responde; se o cache falhar (por exemplo, Redis fora do ar), lê direto do banco.
/// </summary>
/// <param name="leitura">Consulta ao banco.</param>
/// <param name="cache">Cache do painel.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class ObterPainelTransparencia(
    ICampanhaLeitura leitura,
    ICachePainel cache,
    IRelogio relogio,
    ILogger<ObterPainelTransparencia> log)
{
    /// <summary>
    /// Executa a consulta do painel.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As campanhas do painel, ordenadas pela data fim; a lista pode ser vazia.</returns>
    public async Task<IReadOnlyList<PainelItemResponse>> ExecutarAsync(CancellationToken cancelamento)
    {
        Exception? falhaDoBanco = null;

        async Task<IReadOnlyList<PainelItemResponse>> ConsultarAsync(CancellationToken ct)
        {
            try
            {
                var linhas = await leitura.ListarAtivasAsync(relogio.Hoje(), ct);
                return [.. linhas.Select(ParaItem)];
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                falhaDoBanco = excecao;
                throw;
            }
        }

        try
        {
            return await cache.ObterOuCriarAsync(ConsultarAsync, cancelamento);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException && falhaDoBanco is null)
        {
            LogCacheIndisponivel(log, excecao.GetType().Name);
            return await ConsultarAsync(cancelamento);
        }
    }

    /// <summary>
    /// Converte a linha do banco no item do painel, calculando o percentual atingido.
    /// </summary>
    /// <param name="linha">Linha lida do banco.</param>
    /// <returns>O item do painel.</returns>
    private static PainelItemResponse ParaItem(CampanhaDoPainel linha)
    {
        var percentual = linha.MetaFinanceira == 0m
            ? 0m
            : decimal.Round(linha.TotalArrecadado / linha.MetaFinanceira * 100m, 2, MidpointRounding.ToEven);

        return new PainelItemResponse(linha.Id, linha.Titulo, linha.MetaFinanceira, linha.TotalArrecadado, percentual, linha.DataFim);
    }

    /// <summary>
    /// Registra que o cache falhou e a consulta foi direto ao banco, só com o tipo da exceção.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="tipoDaExcecao">Nome do tipo da exceção do cache.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cache do painel indisponível ({TipoDaExcecao}); lendo direto do banco.")]
    private static partial void LogCacheIndisponivel(ILogger log, string tipoDaExcecao);
}
