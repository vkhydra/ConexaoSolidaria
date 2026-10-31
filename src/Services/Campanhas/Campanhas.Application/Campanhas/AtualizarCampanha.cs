using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Caso de uso de edição de campanha ativa, com controle de concorrência otimista pela versão (<c>If-Match</c>).
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class AtualizarCampanha(
    ICampanhaRepositorio campanhas,
    IValidator<CampanhaRequest> validador,
    IRelogio relogio,
    ILogger<AtualizarCampanha> log)
{
    /// <summary>
    /// Executa a edição.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="versaoEsperada">Versão que o gestor viu (valor do <c>If-Match</c>); nula se não foi enviada.</param>
    /// <param name="request">Novos dados da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha com a nova versão, ou o erro: versão obrigatória, validação, não encontrada, conflito de versão ou regra do domínio.</returns>
    public async Task<Result<CampanhaResponse>> ExecutarAsync(CampanhaId id, uint? versaoEsperada, CampanhaRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (versaoEsperada is null)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeCampanha.VersaoObrigatoria);
        }

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeValidacao.Converter(validacao));
        }

        var campanha = await campanhas.ObterPorIdAsync(id, cancelamento);
        if (campanha is null)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeCampanha.NaoEncontrada);
        }

        if (campanha.Versao != versaoEsperada)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeCampanha.ConflitoDeVersao);
        }

        var valores = ValoresDaCampanha.De(request);
        if (valores.EhFalha)
        {
            return Result.Falha<CampanhaResponse>(valores.Erro);
        }

        var atualizada = campanha.Atualizar(request.Titulo, request.Descricao ?? string.Empty, valores.Valor.Periodo, valores.Valor.Meta, relogio.Hoje(), relogio.Agora());
        if (atualizada.EhFalha)
        {
            return Result.Falha<CampanhaResponse>(atualizada.Erro);
        }

        try
        {
            await campanhas.SalvarAsync(campanha, cancelamento);
        }
        catch (ConflitoDeVersaoException)
        {
            return Result.Falha<CampanhaResponse>(ErrosDeCampanha.ConflitoDeVersao);
        }

        LogAtualizada(log, campanha.Id);
        return Result.Sucesso(CampanhaResponse.De(campanha));
    }

    /// <summary>
    /// Registra a edição da campanha, só com o identificador.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="campanhaId">Identificador da campanha editada.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha atualizada: {CampanhaId}.")]
    private static partial void LogAtualizada(ILogger log, CampanhaId campanhaId);
}
