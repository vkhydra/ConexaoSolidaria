using Identity.Domain.Usuarios;

namespace Identity.UnitTests.Usuarios;

/// <summary>
/// Testes do value object <see cref="Cpf"/>: máscara, dígitos verificadores, sequências repetidas e tamanho.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CpfTests
{
    /// <summary>
    /// Dado um CPF válido com ou sem máscara, quando criado, então guarda só os 11 dígitos.
    /// </summary>
    /// <param name="texto">CPF digitado.</param>
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void Criar_CpfValido_GuardaSoDigitos(string texto)
    {
        var resultado = Cpf.Criar(texto);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Valor.ShouldBe(texto.Replace(".", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// Dado um CPF com e sem máscara, quando comparados, então são iguais.
    /// </summary>
    [Fact]
    public void Criar_MesmoCpfComESemMascara_EhIgual()
    {
        Cpf.Criar("529.982.247-25").Valor.ShouldBe(Cpf.Criar("52998224725").Valor);
    }

    /// <summary>
    /// Dado um CPF com dígito verificador errado, sequência repetida, tamanho errado, letras ou máscara torta, quando criado, então é recusado.
    /// </summary>
    /// <param name="texto">CPF digitado.</param>
    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("529.982.247-35")]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    [InlineData("123")]
    [InlineData("5299822472")]
    [InlineData("529982247255")]
    [InlineData("529.982.247-2a")]
    [InlineData("abcdefghijk")]
    [InlineData("529.982.24725")]
    [InlineData(" 52998224725")]
    [InlineData("")]
    [InlineData(null)]
    public void Criar_CpfInvalido_DevolveErro(string? texto)
    {
        var resultado = Cpf.Criar(texto);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Cpf.CodigoInvalido);
        resultado.Erro.Mensagem.ShouldNotContain("529");
    }

    /// <summary>
    /// Dado um CPF válido, quando mascarado, então mostra só os dígitos do meio.
    /// </summary>
    [Fact]
    public void Mascarado_CpfValido_OcultaPrimeirosEUltimosDigitos()
    {
        Cpf.Criar("529.982.247-25").Valor.Mascarado().ShouldBe("***.982.247-**");
    }

    /// <summary>
    /// Dado um CPF válido, quando convertido em texto, então devolve a forma mascarada para não vazar em log.
    /// </summary>
    [Fact]
    public void ToString_CpfValido_DevolveMascarado()
    {
        var texto = Cpf.Criar("52998224725").Valor.ToString();

        texto.ShouldBe("***.982.247-**");
        texto.ShouldNotContain("52998224725");
    }
}
