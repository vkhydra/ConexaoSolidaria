using Identity.Application.Abstracoes;

namespace Identity.UnitTests.Doadores;

/// <summary>
/// Testes da exceção de violação de unicidade.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UnicidadeVioladaExceptionTests
{
    /// <summary>
    /// Dados os construtores padrão, quando usados, então guardam mensagem e causa sem dados pessoais.
    /// </summary>
    [Fact]
    public void Construtores_Padrao_GuardamMensagemECausa()
    {
        var causa = new InvalidOperationException("causa");

        new UnicidadeVioladaException().ShouldNotBeNull();
        new UnicidadeVioladaException("mensagem").Message.ShouldBe("mensagem");
        new UnicidadeVioladaException("mensagem", causa).InnerException.ShouldBe(causa);
    }

    /// <summary>
    /// Dado o campo em conflito, quando a exceção é criada, então expõe o campo e a causa.
    /// </summary>
    [Fact]
    public void Construtor_ComCampo_ExpoeCampo()
    {
        var excecao = new UnicidadeVioladaException(CampoUnico.Cpf, new InvalidOperationException());

        excecao.Campo.ShouldBe(CampoUnico.Cpf);
        excecao.Message.ShouldBe("Violação de unicidade em Cpf.");
    }
}
