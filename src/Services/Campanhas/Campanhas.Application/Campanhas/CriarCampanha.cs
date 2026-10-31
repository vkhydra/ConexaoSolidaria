using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Caso de uso de criação de campanha pelo gestor: valida o formato, aplica as regras do agregado e grava com o evento
/// de alteração na mesma transação.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="metricas">Métricas de campanhas.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class CriarCampanha(
    ICampanhaRepositorio campanhas,
    IValidator<CampanhaRequest> validador,
    IRelogio relogio,
    IMetricasCampanhas metricas,
    ILogger<CriarCampanha> log)
{
    /// <summary>
    /// Executa a criação.
    /// </summary>
    /// <param name="request">Dados da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha criada, <c>validacao</c> ou o erro de regra (422).</returns>
    public async Task<Result<CampanhaResponse>> ExecutarAsync(CampanhaRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeValidacao.Converter(validacao));
        }

        var valores = ValoresDaCampanha.De(request);
        if (valores.EhFalha)
        {
            return Result.Falha<CampanhaResponse>(valores.Erro);
        }

        var campanha = Campanha.Criar(request.Titulo, request.Descricao ?? string.Empty, valores.Valor.Periodo, valores.Valor.Meta, relogio.Hoje(), relogio.Agora());
        if (campanha.EhFalha)
        {
            return Result.Falha<CampanhaResponse>(campanha.Erro);
        }

        await campanhas.AdicionarAsync(campanha.Valor, cancelamento);
        metricas.RegistrarCriacao();
        LogCriada(log, campanha.Valor.Id);
        return Result.Sucesso(CampanhaResponse.De(campanha.Valor));
    }

    /// <summary>
    /// Registra a criação da campanha, só com o identificador.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="campanhaId">Identificador da campanha criada.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha criada: {CampanhaId}.")]
    private static partial void LogCriada(ILogger log, CampanhaId campanhaId);
}
