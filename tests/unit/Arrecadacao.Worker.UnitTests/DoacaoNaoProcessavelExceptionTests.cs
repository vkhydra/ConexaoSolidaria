using Arrecadacao.Worker.Consumidores;

namespace Arrecadacao.Worker.UnitTests;

/// <summary>
/// Testes dos construtores da exceção de falha permanente no processamento de doações.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DoacaoNaoProcessavelExceptionTests
{
    /// <summary>
    /// Dados os três construtores, quando a exceção é criada, então mensagem e causa são guardadas.
    /// </summary>
    [Fact]
    public void Construtores_MensagemECausa_SaoGuardadas()
    {
        var causa = new InvalidOperationException("causa");

        new DoacaoNaoProcessavelException().Message.ShouldNotBeNullOrEmpty();
        new DoacaoNaoProcessavelException("campanha.nao_encontrada").Message.ShouldBe("campanha.nao_encontrada");
        var completa = new DoacaoNaoProcessavelException("doacao.valor_invalido", causa);
        completa.Message.ShouldBe("doacao.valor_invalido");
        completa.InnerException.ShouldBeSameAs(causa);
    }
}
