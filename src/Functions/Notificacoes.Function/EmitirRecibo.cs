using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Notificacoes.Function.Recibos;

namespace Notificacoes.Function;

/// <summary>
/// Função que emite o recibo de uma doação processada. O gatilho RabbitMQ entrega o envelope JSON do MassTransit da fila
/// <c>notificacoes-recibo</c>; a função lê o evento e delega ao <see cref="EmissorRecibo"/>, que deduplica pelo Redis. Falhas
/// sobem como exceção: o runtime repete e, esgotadas as tentativas, a mensagem vai para <c>notificacoes-recibo_error</c>.
/// </summary>
/// <param name="emissor">Emissor do recibo.</param>
/// <param name="log">Logger da função.</param>
public sealed partial class EmitirRecibo(EmissorRecibo emissor, ILogger<EmitirRecibo> log)
{
    /// <summary>
    /// Fila de onde a função consome.
    /// </summary>
    public const string Fila = "notificacoes-recibo";

    /// <summary>
    /// Nome da configuração com a conexão do RabbitMQ (variável de ambiente vinda de um Secret).
    /// </summary>
    public const string ConfiguracaoDaConexao = "RabbitMqConnection";

    /// <summary>
    /// Processa uma mensagem da fila.
    /// </summary>
    /// <param name="envelope">Envelope JSON do MassTransit.</param>
    /// <param name="cancelamento">Token de cancelamento do host.</param>
    /// <returns>Tarefa que termina com a mensagem tratada.</returns>
    /// <exception cref="EnvelopeInvalidoException">O corpo não é um envelope legível.</exception>
    [Function("EmitirRecibo")]
    public async Task ExecutarAsync([RabbitMQTrigger(Fila, ConnectionStringSetting = ConfiguracaoDaConexao)] string envelope, CancellationToken cancelamento)
    {
        var doacao = LeitorEnvelopeMassTransit.Ler(envelope);
        if (doacao is null)
        {
            LogMensagemIgnorada(log);
            return;
        }

        await emissor.EmitirAsync(doacao, cancelamento);
    }

    /// <summary>
    /// Registra que a mensagem era de outro tipo e foi ignorada.
    /// </summary>
    /// <param name="log">Logger.</param>
    [LoggerMessage(EventId = 3, EventName = "MensagemIgnorada", Level = LogLevel.Warning, Message = "MensagemIgnorada: o envelope não é uma doação processada")]
    private static partial void LogMensagemIgnorada(ILogger log);
}
