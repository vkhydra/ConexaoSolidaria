using System.Text.Json;
using System.Text.Json.Serialization;
using ConexaoSolidaria.Contracts.V1;

namespace Notificacoes.Function.Recibos;

/// <summary>
/// Lê o envelope JSON do MassTransit entregue pela fila e extrai o <see cref="DoacaoProcessadaEvent"/>. A função não usa o
/// MassTransit; ela só conhece o formato do envelope: <c>messageType</c> (lista de URNs) e <c>message</c> (o evento, em camelCase,
/// com <c>decimal</c> escrito como texto).
/// </summary>
public static class LeitorEnvelopeMassTransit
{
    /// <summary>
    /// URN do único tipo de mensagem que a função aceita.
    /// </summary>
    public const string TipoEsperado = "urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent";

    /// <summary>
    /// Opções de leitura: camelCase e números aceitos também como texto.
    /// </summary>
    private static readonly JsonSerializerOptions _opcoes = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>
    /// Lê o envelope.
    /// </summary>
    /// <param name="envelope">Corpo da mensagem, em JSON.</param>
    /// <returns>O evento, ou nulo se o envelope é de outro tipo de mensagem (que a função deve ignorar).</returns>
    /// <exception cref="EnvelopeInvalidoException">O JSON é inválido, ou o envelope não tem <c>messageType</c> e <c>message</c> legíveis.</exception>
    public static DoacaoProcessadaEvent? Ler(string envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope))
        {
            throw new EnvelopeInvalidoException("O envelope está vazio.");
        }

        try
        {
            using var documento = JsonDocument.Parse(envelope);
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object
                || !raiz.TryGetProperty("messageType", out var tipos) || tipos.ValueKind != JsonValueKind.Array
                || !raiz.TryGetProperty("message", out var mensagem) || mensagem.ValueKind != JsonValueKind.Object)
            {
                throw new EnvelopeInvalidoException("O envelope não tem messageType e message.");
            }

            if (!tipos.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == TipoEsperado))
            {
                return null;
            }

            return mensagem.Deserialize<DoacaoProcessadaEvent>(_opcoes)
                ?? throw new EnvelopeInvalidoException("A mensagem do envelope está vazia.");
        }
        catch (JsonException excecao)
        {
            throw new EnvelopeInvalidoException("O envelope não é um JSON válido ou a mensagem não tem os campos esperados.", excecao);
        }
    }
}
