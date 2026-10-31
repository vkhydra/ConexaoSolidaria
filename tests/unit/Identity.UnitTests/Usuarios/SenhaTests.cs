using Identity.Domain.Usuarios;

namespace Identity.UnitTests.Usuarios;

/// <summary>
/// Testes do value object <see cref="Senha"/>: política de tamanho e de classes de caracteres.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SenhaTests
{
    /// <summary>
    /// Dada uma senha que cumpre a política, quando criada, então é aceita.
    /// </summary>
    /// <param name="texto">Senha digitada.</param>
    [Theory]
    [InlineData("Senha@123")]
    [InlineData("abcdef1!")]
    [InlineData("Ação#2026ok")]
    public void Criar_SenhaForte_Aceita(string texto)
    {
        var resultado = Senha.Criar(texto);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Valor.ShouldBe(texto);
    }

    /// <summary>
    /// Dado o limite de 64 caracteres, quando a senha tem exatamente 64 ou 65, então 64 passa e 65 é recusada.
    /// </summary>
    [Fact]
    public void Criar_LimiteMaximo_AceitaSessentaEQuatroERecusaSessentaECinco()
    {
        var base64 = new string('a', Senha.TamanhoMaximo - 2) + "1@";

        Senha.Criar(base64).EhSucesso.ShouldBeTrue();
        Senha.Criar(base64 + "a").EhFalha.ShouldBeTrue();
    }

    /// <summary>
    /// Dada uma senha fraca (curta, sem número, sem especial, sem letra ou ausente), quando criada, então devolve senha.fraca.
    /// </summary>
    /// <param name="texto">Senha digitada.</param>
    [Theory]
    [InlineData("curta@1")]
    [InlineData("semnumero@")]
    [InlineData("Sem1Especial")]
    [InlineData("12345678@")]
    [InlineData("")]
    [InlineData(null)]
    public void Criar_SenhaFraca_DevolveErro(string? texto)
    {
        var resultado = Senha.Criar(texto);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Senha.CodigoFraca);
        resultado.Erro.Mensagem.ShouldBe(Senha.MensagemFraca);
    }

    /// <summary>
    /// Dada uma senha válida, quando convertida em texto, então não a revela.
    /// </summary>
    [Fact]
    public void ToString_SenhaValida_NaoRevelaSenha()
    {
        Senha.Criar("Senha@123").Valor.ToString().ShouldBe("***");
    }
}
