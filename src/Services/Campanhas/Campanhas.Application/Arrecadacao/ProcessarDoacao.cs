using Campanhas.Application.Abstracoes;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Transparencia;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Arrecadacao;

/// <summary>
/// Resultado do processamento de uma doação.
/// </summary>
/// <param name="JaProcessada">Verdadeiro quando era uma reentrega e o total não foi alterado de novo.</param>
/// <param name="ProcessadaEm">Instante em que a doação foi somada ao total.</param>
/// <param name="TotalArrecadado">Total da campanha depois da doação (o atual, em caso de reentrega).</param>
public sealed record DoacaoProcessadaResultado(bool JaProcessada, DateTimeOffset ProcessadaEm, decimal TotalArrecadado);

/// <summary>
/// Caso de uso do Arrecadacao.Worker: soma uma doação recebida ao total da campanha, uma única vez, mesmo que a mensagem
/// chegue de novo. As falhas de negócio (valor inválido, campanha inexistente) são permanentes e não adiantam retentativa.
/// </summary>
/// <param name="registro">Porta que soma a doação ao total.</param>
/// <param name="livro">Livro de doações, gravado também na reentrega para completar um processamento interrompido.</param>
/// <param name="painel">Invalidador do cache do painel de transparência, para o novo total aparecer sem esperar a validade.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class ProcessarDoacao(IRegistroArrecadacao registro, ILivroDoacoes livro, InvalidadorDoPainel painel, IRelogio relogio, ILogger<ProcessarDoacao> log)
{
    /// <summary>
    /// Executa o processamento.
    /// </summary>
    /// <param name="doacao">Doação recebida, vinda da fila.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O resultado, <c>doacao.valor_invalido</c> ou <c>campanha.nao_encontrada</c> (ambos permanentes).</returns>
    public async Task<Result<DoacaoProcessadaResultado>> ExecutarAsync(DoacaoRecebidaEvent doacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(doacao);

        if (doacao.Valor <= 0m)
        {
            LogValorInvalido(log, doacao.DoacaoId);
            return Result.Falha<DoacaoProcessadaResultado>(new Erro(Campanha.CodigoDoacaoValorInvalido, "O valor da doação deve ser maior que zero."));
        }

        var registrada = await registro.RegistrarAsync(doacao, relogio.Agora(), cancelamento);
        switch (registrada.Situacao)
        {
            case SituacaoRegistro.CampanhaInexistente:
                LogCampanhaInexistente(log, doacao.DoacaoId, doacao.CampanhaId);
                return Result.Falha<DoacaoProcessadaResultado>(ErrosDeCampanha.NaoEncontrada);
            case SituacaoRegistro.JaProcessada:
                LogJaProcessada(log, doacao.DoacaoId);
                break;
            default:
                LogProcessada(log, doacao.DoacaoId, doacao.CampanhaId);
                break;
        }

        // O livro fica fora da transação do PostgreSQL (não há transação entre os dois bancos). Por isso é gravado sempre,
        // inclusive na reentrega: se a primeira tentativa caiu depois do COMMIT, a retentativa completa o livro.
        await livro.RegistrarAsync(
            new LancamentoDoacao(doacao.DoacaoId, doacao.CampanhaId, doacao.DoadorId, doacao.Valor, registrada.TotalArrecadado!.Value, doacao.OcorridoEm, registrada.ProcessadaEm!.Value),
            cancelamento);

        // Também na reentrega: se a primeira tentativa caiu antes de invalidar, o painel ainda mostraria o total antigo.
        await painel.InvalidarAsync(cancelamento);

        return Result.Sucesso(new DoacaoProcessadaResultado(
            registrada.Situacao == SituacaoRegistro.JaProcessada,
            registrada.ProcessadaEm!.Value,
            registrada.TotalArrecadado!.Value));
    }

    /// <summary>
    /// Registra a doação somada ao total, só com identificadores.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="campanhaId">Identificador da campanha.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Doação {DoacaoId} somada ao total da campanha {CampanhaId}.")]
    private static partial void LogProcessada(ILogger log, Guid doacaoId, Guid campanhaId);

    /// <summary>
    /// Registra a reentrega de uma doação já somada.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Doação {DoacaoId} já havia sido somada; total mantido.")]
    private static partial void LogJaProcessada(ILogger log, Guid doacaoId);

    /// <summary>
    /// Registra a doação recusada por valor inválido.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Doação {DoacaoId} com valor inválido; não será processada.")]
    private static partial void LogValorInvalido(ILogger log, Guid doacaoId);

    /// <summary>
    /// Registra a doação de campanha inexistente.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="campanhaId">Identificador da campanha inexistente.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Doação {DoacaoId} aponta para a campanha inexistente {CampanhaId}.")]
    private static partial void LogCampanhaInexistente(ILogger log, Guid doacaoId, Guid campanhaId);
}
