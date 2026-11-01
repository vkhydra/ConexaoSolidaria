using Notificacoes.Function.Recibos;

namespace Notificacoes.Function.UnitTests;

/// <summary>
/// Testes do <see cref="LeitorEnvelopeMassTransit"/> com um envelope no formato real gravado pelo MassTransit 8.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LeitorEnvelopeMassTransitTests
{
    /// <summary>
    /// Envelope de uma doação processada, com <c>decimal</c> como texto, como o MassTransit o escreve.
    /// </summary>
    private const string _envelopeValido = """
        {
          "messageId": "b0120000-7608-ea42-ef1e-08df37bc3a58",
          "requestId": null,
          "messageType": [ "urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent" ],
          "message": {
            "doacaoId": "ac2ccc8d-0f2e-4cb9-a599-c4c7c7340786",
            "campanhaId": "707b8017-c21a-4af7-85a3-5cbe66a7242f",
            "doadorId": "4d8ea5e8-d8db-412e-8f0c-128a16f29ce8",
            "valor": "150.00",
            "totalArrecadado": "1150.00",
            "recebidaEm": "2026-11-10T17:10:05.123+00:00",
            "processadaEm": "2026-11-10T17:10:07.123+00:00"
          },
          "headers": {},
          "host": { "machineName": "x" }
        }
        """;

    /// <summary>
    /// Dado um envelope válido, quando lido, então o evento traz todos os campos, com o valor lido do texto.
    /// </summary>
    [Fact]
    public void Ler_EnvelopeValido_DevolveOEvento()
    {
        var evento = LeitorEnvelopeMassTransit.Ler(_envelopeValido);

        evento.ShouldNotBeNull();
        evento.DoacaoId.ShouldBe(Guid.Parse("ac2ccc8d-0f2e-4cb9-a599-c4c7c7340786"));
        evento.CampanhaId.ShouldBe(Guid.Parse("707b8017-c21a-4af7-85a3-5cbe66a7242f"));
        evento.DoadorId.ShouldBe(Guid.Parse("4d8ea5e8-d8db-412e-8f0c-128a16f29ce8"));
        evento.Valor.ShouldBe(150.00m);
        evento.TotalArrecadado.ShouldBe(1150.00m);
        evento.ProcessadaEm.ShouldBe(new DateTimeOffset(2026, 11, 10, 17, 10, 7, 123, TimeSpan.Zero));
    }

    /// <summary>
    /// Dado um envelope com o valor como número JSON, quando lido, então também é aceito.
    /// </summary>
    [Fact]
    public void Ler_ValorComoNumero_Aceita()
    {
        var envelope = _envelopeValido.Replace("\"150.00\"", "150.00", StringComparison.Ordinal);

        LeitorEnvelopeMassTransit.Ler(envelope)!.Valor.ShouldBe(150.00m);
    }

    /// <summary>
    /// Dado um envelope de outro tipo de mensagem, quando lido, então devolve nulo (a função ignora).
    /// </summary>
    [Fact]
    public void Ler_TipoErrado_DevolveNulo()
    {
        var envelope = _envelopeValido.Replace("DoacaoProcessadaEvent", "DoacaoRecebidaEvent", StringComparison.Ordinal);

        LeitorEnvelopeMassTransit.Ler(envelope).ShouldBeNull();
    }

    /// <summary>
    /// Dado um envelope malformado ou sem os campos obrigatórios, quando lido, então lança <see cref="EnvelopeInvalidoException"/>.
    /// </summary>
    /// <param name="envelope">Corpo da mensagem.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ isto nao e json")]
    [InlineData("[1, 2]")]
    [InlineData("{ \"message\": {} }")]
    [InlineData("{ \"messageType\": [] }")]
    [InlineData("{ \"messageType\": \"texto\", \"message\": {} }")]
    [InlineData("{ \"messageType\": [\"urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent\"], \"message\": [] }")]
    [InlineData("{ \"messageType\": [\"urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent\"], \"message\": { \"doacaoId\": \"nao-e-guid\" } }")]
    public void Ler_EnvelopeInvalido_LancaExcecao(string envelope)
    {
        Should.Throw<EnvelopeInvalidoException>(() => LeitorEnvelopeMassTransit.Ler(envelope));
    }

    /// <summary>
    /// Dado um envelope com tipos misturados (um deles é o esperado), quando lido, então é aceito.
    /// </summary>
    [Fact]
    public void Ler_TiposMistos_AceitaSeUmDelesEOEsperado()
    {
        var envelope = _envelopeValido.Replace("[ \"urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent\" ]", "[ 7, \"urn:message:Outro\", \"urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent\" ]", StringComparison.Ordinal);

        LeitorEnvelopeMassTransit.Ler(envelope).ShouldNotBeNull();
    }

    /// <summary>
    /// Dados os três construtores, quando a exceção é criada, então mensagem e causa são guardadas.
    /// </summary>
    [Fact]
    public void EnvelopeInvalidoException_Construtores_GuardamMensagemECausa()
    {
        var causa = new InvalidOperationException("causa");

        new EnvelopeInvalidoException().Message.ShouldNotBeNullOrEmpty();
        new EnvelopeInvalidoException("ruim").Message.ShouldBe("ruim");
        new EnvelopeInvalidoException("ruim", causa).InnerException.ShouldBeSameAs(causa);
    }
}
