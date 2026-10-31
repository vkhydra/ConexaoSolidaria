using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
/// Caso de uso de recebimento de doação: confere o formato, trata a chave de idempotência, confirma que a campanha aceita
/// doação hoje e publica o evento pelo Outbox. O total arrecadado não muda aqui; quem o escreve é o Arrecadacao.Worker.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="publicador">Publicador das doações aceitas.</param>
/// <param name="chaves">Consulta das chaves de idempotência.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio no fuso de São Paulo.</param>
/// <param name="metricas">Métricas de doações recebidas e recusadas.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class ReceberDoacao(
    ICampanhaRepositorio campanhas,
    IPublicadorDeDoacoes publicador,
    IChavesIdempotencia chaves,
    IValidator<DoacaoRequest> validador,
    IRelogio relogio,
    IMetricasDoacoes metricas,
    ILogger<ReceberDoacao> log)
{
    /// <summary>
    /// Nome do cabeçalho da chave de idempotência, usado como campo no erro de validação.
    /// </summary>
    public const string CabecalhoChave = "Idempotency-Key";

    /// <summary>
    /// Tamanho máximo da chave de idempotência.
    /// </summary>
    public const int ChaveTamanhoMaximo = 100;

    /// <summary>
    /// Executa o recebimento.
    /// </summary>
    /// <param name="doadorId">Doador autenticado, vindo do token.</param>
    /// <param name="request">Dados da doação.</param>
    /// <param name="chaveIdempotencia">Valor do cabeçalho <c>Idempotency-Key</c>; nulo quando não foi enviado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>
    /// O aceite (202; repetido com a mesma chave e o mesmo corpo, devolve a doação original), <c>validacao</c>,
    /// <c>campanha.nao_encontrada</c>, <c>doacao.campanha_nao_ativa</c> ou <c>doacao.chave_idempotencia_conflitante</c>.
    /// </returns>
    public async Task<Result<DoacaoAceitaResponse>> ExecutarAsync(Guid doadorId, DoacaoRequest request, string? chaveIdempotencia, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            metricas.RegistrarRecusada(MotivoRecusa.Validacao);
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeValidacao.Converter(validacao));
        }

        if (chaveIdempotencia is not null && (string.IsNullOrWhiteSpace(chaveIdempotencia) || chaveIdempotencia.Length > ChaveTamanhoMaximo))
        {
            metricas.RegistrarRecusada(MotivoRecusa.Validacao);
            return Result.Falha<DoacaoAceitaResponse>(ChaveInvalida());
        }

        var campanhaId = new CampanhaId(request.IdCampanha!.Value);
        var valor = Dinheiro.Criar(request.ValorDoacao!.Value).Valor;
        var chave = chaveIdempotencia is null ? null : new ChaveParaRegistrar(chaveIdempotencia, CalcularHash(campanhaId, valor));

        if (chave is not null)
        {
            var existente = await chaves.ObterAsync(chave.Chave, doadorId, cancelamento);
            if (existente is not null)
            {
                return Reaproveitar(existente, chave);
            }
        }

        var campanha = await campanhas.ObterPorIdAsync(campanhaId, cancelamento);
        if (campanha is null)
        {
            metricas.RegistrarRecusada(MotivoRecusa.CampanhaNaoEncontrada);
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeCampanha.NaoEncontrada);
        }

        if (!campanha.PodeReceberDoacao(relogio.Hoje()))
        {
            LogRecusada(log, campanhaId);
            metricas.RegistrarRecusada(MotivoRecusa.CampanhaNaoAtiva);
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeDoacao.CampanhaNaoAtiva);
        }

        var doacaoId = Guid.CreateVersion7();
        try
        {
            await publicador.PublicarRecebidaAsync(new DoacaoRecebidaEvent(doacaoId, campanhaId.Valor, doadorId, valor.Valor, relogio.Agora()), chave, cancelamento);
        }
        catch (ChaveJaRegistradaException) when (chave is not null)
        {
            var vencedora = await chaves.ObterAsync(chave.Chave, doadorId, cancelamento);
            if (vencedora is null)
            {
                throw;
            }

            return Reaproveitar(vencedora, chave);
        }

        metricas.RegistrarRecebida();
        LogAceita(log, doacaoId, campanhaId);
        return Result.Sucesso(Aceite(doacaoId));
    }

    /// <summary>
    /// Calcula o SHA-256 do corpo em forma canônica (campanha e valor com duas casas), que não depende do formato do JSON enviado.
    /// </summary>
    /// <param name="campanhaId">Campanha do pedido.</param>
    /// <param name="valor">Valor do pedido.</param>
    /// <returns>O hash de 32 bytes.</returns>
    private static byte[] CalcularHash(CampanhaId campanhaId, Dinheiro valor)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{campanhaId.Valor:D}|{valor.Valor:F2}")));
    }

    /// <summary>
    /// Monta o aceite da doação.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <returns>A resposta de aceite.</returns>
    private static DoacaoAceitaResponse Aceite(Guid doacaoId)
    {
        return new DoacaoAceitaResponse(doacaoId, "Recebida", "Doação recebida. O total será atualizado em instantes.");
    }

    /// <summary>
    /// Monta o erro de validação do cabeçalho de idempotência.
    /// </summary>
    /// <returns>O erro <c>validacao</c> com o campo <c>Idempotency-Key</c>.</returns>
    private static Erro ChaveInvalida()
    {
        return new Erro(ErrosDeValidacao.CodigoValidacao, "Dados inválidos.")
        {
            Campos = new Dictionary<string, string[]> { [CabecalhoChave] = [$"A Idempotency-Key deve ter de 1 a {ChaveTamanhoMaximo} caracteres."] },
        };
    }

    /// <summary>
    /// Decide o que fazer com uma chave já registrada: mesmo corpo devolve a doação original; outro corpo é conflito.
    /// </summary>
    /// <param name="existente">Chave registrada na primeira requisição.</param>
    /// <param name="atual">Chave e hash da requisição atual.</param>
    /// <returns>O aceite da doação original ou <c>doacao.chave_idempotencia_conflitante</c>.</returns>
    private Result<DoacaoAceitaResponse> Reaproveitar(ChaveRegistrada existente, ChaveParaRegistrar atual)
    {
        if (!CryptographicOperations.FixedTimeEquals(existente.HashCorpo, atual.HashCorpo))
        {
            LogChaveConflitante(log, existente.DoacaoId);
            metricas.RegistrarRecusada(MotivoRecusa.ChaveConflitante);
            return Result.Falha<DoacaoAceitaResponse>(ErrosDeDoacao.ChaveConflitante);
        }

        LogRepetida(log, existente.DoacaoId);
        return Result.Sucesso(Aceite(existente.DoacaoId));
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

    /// <summary>
    /// Registra a repetição de uma requisição já aceita, que devolve a doação original.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação original.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Requisição repetida: devolvida a doação {DoacaoId}.")]
    private static partial void LogRepetida(ILogger log, Guid doacaoId);

    /// <summary>
    /// Registra o reúso de uma chave com outro corpo.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="doacaoId">Identificador da doação que registrou a chave.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Chave de idempotência reutilizada com outro corpo; doação original {DoacaoId}.")]
    private static partial void LogChaveConflitante(ILogger log, Guid doacaoId);
}
