using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Pedido de mudança de status.
/// </summary>
/// <param name="Status">Status de destino: <c>Concluida</c> ou <c>Cancelada</c> (<c>Ativa</c> é conhecido, mas nunca é destino válido).</param>
public sealed record AlterarStatusRequest(string? Status);

/// <summary>
/// Valida o nome do status enviado: precisa ser um status conhecido.
/// </summary>
public sealed class AlterarStatusRequestValidator : AbstractValidator<AlterarStatusRequest>
{
    /// <summary>
    /// Inicializa a regra do status.
    /// </summary>
    public AlterarStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("O status é obrigatório.")
            .Must(status => Enum.TryParse<StatusCampanha>(status, ignoreCase: false, out var valor) && Enum.IsDefined(valor))
            .WithMessage("Status desconhecido. Use Concluida ou Cancelada.")
            .When(x => !string.IsNullOrEmpty(x.Status), ApplyConditionTo.CurrentValidator);
    }
}

/// <summary>
/// Caso de uso de mudança de status: conclui ou cancela uma campanha ativa. As únicas transições são
/// <c>Ativa → Concluida</c> e <c>Ativa → Cancelada</c>.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class AlterarStatusCampanha(
    ICampanhaRepositorio campanhas,
    IValidator<AlterarStatusRequest> validador,
    IRelogio relogio,
    ILogger<AlterarStatusCampanha> log)
{
    /// <summary>
    /// Executa a mudança de status.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="request">Status de destino.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Sucesso, ou validação, não encontrada, conflito de versão ou <c>campanha.transicao_invalida</c>.</returns>
    public async Task<Result> ExecutarAsync(CampanhaId id, AlterarStatusRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha(ErrosDeValidacao.Converter(validacao));
        }

        var campanha = await campanhas.ObterPorIdAsync(id, cancelamento);
        if (campanha is null)
        {
            return Result.Falha(ErrosDeCampanha.NaoEncontrada);
        }

        var destino = Enum.Parse<StatusCampanha>(request.Status!);
        var agora = relogio.Agora();
        var resultado = destino switch
        {
            StatusCampanha.Concluida => campanha.Concluir(agora),
            StatusCampanha.Cancelada => campanha.Cancelar(agora),
            _ => Result.Falha(new Erro(Campanha.CodigoTransicaoInvalida, "Uma campanha não pode voltar a ficar ativa.")),
        };
        if (resultado.EhFalha)
        {
            return resultado;
        }

        try
        {
            await campanhas.SalvarAsync(campanha, cancelamento);
        }
        catch (ConflitoDeVersaoException)
        {
            return Result.Falha(ErrosDeCampanha.ConflitoDeVersao);
        }

        LogStatusAlterado(log, campanha.Id, destino);
        return Result.Ok();
    }

    /// <summary>
    /// Registra a mudança de status, só com o identificador e o novo status.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <param name="status">Novo status.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha {CampanhaId} passou para {Status}.")]
    private static partial void LogStatusAlterado(ILogger log, CampanhaId campanhaId, StatusCampanha status);
}
