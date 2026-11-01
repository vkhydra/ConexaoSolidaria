extern alias FuncaoApp;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Ambiente de um cenário para a Notificacoes.Function: a função de produção (leitor de envelope, emissor e deduplicação no
/// Redis real) com o logger trocado por um que captura os registros. O gatilho do RabbitMQ é substituído por
/// <see cref="ReceberAsync"/>, que entrega à função o envelope que a fila entregaria.
/// </summary>
public sealed partial class AmbienteRecibo : IAsyncDisposable
{
    /// <summary>
    /// Campos que um recibo pode ter; qualquer outro é dado a mais que não deveria estar no log.
    /// </summary>
    private static readonly string[] _camposPermitidos = ["CampanhaId", "DoacaoId", "DoadorId", "TraceId", "Valor"];

    /// <summary>
    /// Logger que guarda os registros do emissor.
    /// </summary>
    private readonly LoggerDeRecibo _log = new();

    /// <summary>
    /// Conexão com o Redis do cenário.
    /// </summary>
    private readonly Lazy<Task<ConnectionMultiplexer>> _redis = new(() => ConnectionMultiplexer.ConnectAsync(ServidorRedis.Conexao));

    /// <summary>
    /// Função de recibo, criada na primeira entrega.
    /// </summary>
    private FuncaoApp::Notificacoes.Function.EmitirRecibo? _funcao;

    /// <summary>
    /// Entrega um envelope à função, como o gatilho RabbitMQ faria.
    /// </summary>
    /// <param name="envelope">Corpo JSON do MassTransit.</param>
    /// <returns>Tarefa que termina com a mensagem tratada.</returns>
    public async Task ReceberAsync(string envelope)
    {
        if (_funcao is null)
        {
            var deduplicador = new FuncaoApp::Notificacoes.Function.Recibos.DeduplicadorRecibosRedis(await _redis.Value);
            _funcao = new FuncaoApp::Notificacoes.Function.EmitirRecibo(new FuncaoApp::Notificacoes.Function.Recibos.EmissorRecibo(deduplicador, _log), Microsoft.Extensions.Logging.Abstractions.NullLogger<FuncaoApp::Notificacoes.Function.EmitirRecibo>.Instance);
        }

        await _funcao.ExecutarAsync(envelope, CancellationToken.None);
    }

    /// <summary>
    /// Obtém os recibos emitidos para a doação.
    /// </summary>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <returns>Os registros <c>ReciboEmitido</c> dela.</returns>
    public IReadOnlyList<RegistroDeRecibo> RecibosDe(Guid doacaoId)
    {
        return [.. _log.Registros.Where(r => r.NomeDoEvento == "ReciboEmitido" && (Guid)r.Campos["DoacaoId"]! == doacaoId)];
    }

    /// <summary>
    /// Confere que o registro só tem identificadores e valor: campos fora da lista permitida, e-mail ou CPF no texto reprovam.
    /// </summary>
    /// <param name="registro">Registro do recibo.</param>
    /// <returns>Os problemas encontrados; vazio quando o registro está limpo.</returns>
    public static IReadOnlyList<string> ProblemasDeDadosPessoais(RegistroDeRecibo registro)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var problemas = new List<string>();
        problemas.AddRange(registro.Campos.Keys.Except(_camposPermitidos).Select(c => $"campo não permitido: {c}"));
        var texto = registro.Mensagem + " " + string.Join(' ', registro.Campos.Values.Select(v => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)));
        if (Email().IsMatch(texto))
        {
            problemas.Add("o registro contém um e-mail");
        }

        if (Cpf().IsMatch(texto))
        {
            problemas.Add("o registro contém um CPF");
        }

        return problemas;
    }

    /// <summary>
    /// Libera a conexão com o Redis.
    /// </summary>
    /// <returns>Tarefa que termina com a conexão liberada.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_redis.IsValueCreated)
        {
            await (await _redis.Value).DisposeAsync();
        }
    }

    /// <summary>
    /// Expressão que reconhece um e-mail.
    /// </summary>
    /// <returns>A expressão regular.</returns>
    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+")]
    private static partial Regex Email();

    /// <summary>
    /// Expressão que reconhece um CPF, com ou sem pontuação.
    /// </summary>
    /// <returns>A expressão regular.</returns>
    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b")]
    private static partial Regex Cpf();

    /// <summary>
    /// Logger do emissor que guarda cada registro.
    /// </summary>
    private sealed class LoggerDeRecibo : ILogger<FuncaoApp::Notificacoes.Function.Recibos.EmissorRecibo>
    {
        /// <summary>
        /// Obtém os registros recebidos.
        /// </summary>
        public List<RegistroDeRecibo> Registros { get; } = [];

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            var campos = state is IEnumerable<KeyValuePair<string, object?>> pares
                ? pares.Where(p => p.Key != "{OriginalFormat}").ToDictionary(p => p.Key, p => p.Value)
                : [];
            lock (Registros)
            {
                Registros.Add(new RegistroDeRecibo(eventId.Name, formatter(state, exception), campos));
            }
        }
    }
}

/// <summary>
/// Um registro de log do emissor capturado no cenário.
/// </summary>
/// <param name="NomeDoEvento">Nome do evento de log (<c>ReciboEmitido</c> ou <c>ReciboDuplicadoIgnorado</c>).</param>
/// <param name="Mensagem">Mensagem já formatada.</param>
/// <param name="Campos">Campos estruturados do registro.</param>
public sealed record RegistroDeRecibo(string? NomeDoEvento, string Mensagem, IReadOnlyDictionary<string, object?> Campos);
