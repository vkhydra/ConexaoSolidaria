using Identity.Domain.Usuarios;

namespace Identity.UnitTests.Usuarios;

/// <summary>
/// Testes do value object <see cref="Consentimento"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ConsentimentoTests
{
    /// <summary>
    /// Instante fixo do aceite.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dado o aceite com versão, quando registrado, então guarda a versão e a data.
    /// </summary>
    [Fact]
    public void Criar_ComAceiteEVersao_GuardaVersaoEData()
    {
        var resultado = Consentimento.Criar(true, "2026-10", _agora);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Versao.ShouldBe("2026-10");
        resultado.Valor.Em.ShouldBe(_agora);
    }

    /// <summary>
    /// Dado um aceite negado, versão ausente, em branco ou grande demais, quando registrado, então devolve consentimento.obrigatorio.
    /// </summary>
    /// <param name="aceitou">Se o visitante aceitou.</param>
    /// <param name="versao">Versão da política.</param>
    [Theory]
    [InlineData(false, "2026-10")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    [InlineData(true, "012345678901234567890")]
    public void Criar_SemAceiteOuVersaoInvalida_DevolveErro(bool aceitou, string? versao)
    {
        var resultado = Consentimento.Criar(aceitou, versao, _agora);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Consentimento.CodigoObrigatorio);
    }

    /// <summary>
    /// Dados dois aceites iguais, quando comparados, então são iguais por valor.
    /// </summary>
    [Fact]
    public void Igualdade_MesmaVersaoEData_EhIgual()
    {
        Consentimento.Criar(true, "2026-10", _agora).Valor.ShouldBe(Consentimento.Criar(true, "2026-10", _agora).Valor);
    }
}
