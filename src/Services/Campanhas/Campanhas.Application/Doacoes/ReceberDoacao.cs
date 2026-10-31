using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using ConexaoSolidaria.Contracts.V1;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Doacoes;

/// <summary>
/// Caso de uso de recebimento de doação: confere o formato, confirma que a campanha aceita doação hoje e publica o evento
/// pelo Outbox. O total arrecadado não muda aqui; quem o escreve é o Arrecadacao.Worker.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="publicador">Publicador das doações aceitas.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class ReceberDoacao(
    ICampanhaRepositorio campanhas,
    IPublicadorDeDoacoes publicador,
    IValidator<DoacaoRequest> validador,
    IRelogio relogio,
    ILogger<ReceberDoacao> log)
{
    /// <summary>
    /// Executa o recebimento.
    /// </summary>
    /// <param name="doadorId">Doador autenticado, vindo do token.</param>
    /// <param name="request">Dados da doação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O aceite (202), <c>validacao</c>, <c>campanha.nao_encontrada</c> ou <c>doacao.campanha_nao_ativa</c>.</returns>
    public async Task<Result<DoacaoAceitaResponse>> ExecutarAsync(Guid doadorId, DoacaoRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeValidacao.Converter(validacao));
        }

        var campanhaId = new CampanhaId(request.IdCampanha!.Value);
        var campanha = await campanhas.ObterPorIdAsync(campanhaId, cancelamento);
        if (campanha is null)
        {
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeCampanha.NaoEncontrada);
        }

        if (!campanha.PodeReceberDoacao(relogio.Hoje()))
        {
            LogRecusada(log, campanhaId);
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeDoacao.CampanhaNaoAtiva);
        }

        var doacaoId = Guid.CreateVersion7();
        var valor = Dinheiro.Criar(request.ValorDoacao!.Value).Valor;
        await publicador.PublicarRecebidaAsync(new DoacaoRecebidaEvent(doacaoId, campanhaId.Valor, doadorId, valor.Valor, relogio.Agora()), cancelamento);

        LogAceita(log, doacaoId, campanhaId);
        return Result.Sucesso(new DoacaoAceitaResponse(doacaoId, "Recebida", "Doação recebida. O total será atualizado em instantes."));
    }

    /// <summary>
    /// Registra a doação aceita, só com identificadores.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="campanhaId">Identificador da campanha.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Doação {DoacaoId} aceita para a campanha {CampanhaId}.")]
    private static partial void LogAceita(ILogger log, Guid doacaoId, CampanhaId campanhaId);

    /// <summary>
    /// Registra a doação recusada porque a campanha não está apta, só com o identificador da campanha.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="campanhaId">Identificador da campanha.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Doação recusada: a campanha {CampanhaId} não está apta.")]
    private static partial void LogRecusada(ILogger log, CampanhaId campanhaId);
}
