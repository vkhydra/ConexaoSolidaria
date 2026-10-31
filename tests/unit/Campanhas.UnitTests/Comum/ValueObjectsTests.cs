using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Comum;

/// <summary>
/// Testes dos value objects <see cref="Dinheiro"/> e <see cref="Periodo"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ValueObjectsTests
{
    /// <summary>
    /// Dado um valor com duas casas, quando criado, então é aceito sem alteração.
    /// </summary>
    [Fact]
    public void Dinheiro_Criar_ValorComDuasCasas_Aceita()
    {
        var resultado = Dinheiro.Criar(10000.00m);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Valor.ShouldBe(10000.00m);
        resultado.Valor.ToString().ShouldBe("10000.00");
    }

    /// <summary>
    /// Dado um valor com mais de duas casas, quando criado, então usa o arredondamento bancário.
    /// </summary>
    /// <param name="entrada">Valor informado.</param>
    /// <param name="esperado">Valor arredondado esperado.</param>
    [Theory]
    [InlineData(1.005, 1.00)]
    [InlineData(1.015, 1.02)]
    [InlineData(1.025, 1.02)]
    [InlineData(2.3449, 2.34)]
    [InlineData(2.3451, 2.35)]
    public void Dinheiro_Criar_MaisDeDuasCasas_ArredondaParaOPar(double entrada, double esperado)
    {
        Dinheiro.Criar((decimal)entrada).Valor.Valor.ShouldBe((decimal)esperado);
    }

    /// <summary>
    /// Dado um valor negativo, quando criado, então devolve dinheiro.negativo; zero é aceito.
    /// </summary>
    [Fact]
    public void Dinheiro_Criar_Negativo_DevolveErro()
    {
        var negativo = Dinheiro.Criar(-0.01m);

        negativo.EhFalha.ShouldBeTrue();
        negativo.Erro.Codigo.ShouldBe(Dinheiro.CodigoNegativo);
        Dinheiro.Criar(0m).EhSucesso.ShouldBeTrue();
        Dinheiro.Criar(-0.004m).Valor.ShouldBe(Dinheiro.Zero);
    }

    /// <summary>
    /// Dados dois valores, quando somados, então a soma mantém duas casas; somar nulo é recusado.
    /// </summary>
    [Fact]
    public void Dinheiro_Somar_SomaDoisValores()
    {
        var soma = Dinheiro.Zero.Somar(Dinheiro.Criar(10.10m).Valor).Somar(Dinheiro.Criar(0.20m).Valor);

        soma.Valor.ShouldBe(10.30m);
        Should.Throw<ArgumentNullException>(() => soma.Somar(null!));
    }

    /// <summary>
    /// Dados dois valores iguais, quando comparados, então são iguais por valor.
    /// </summary>
    [Fact]
    public void Dinheiro_Igualdade_PorValor()
    {
        Dinheiro.Criar(5m).Valor.ShouldBe(Dinheiro.Criar(5.00m).Valor);
    }

    /// <summary>
    /// Dadas datas em ordem, quando o período é criado, então aceita inclusive início e fim no mesmo dia.
    /// </summary>
    [Fact]
    public void Periodo_Criar_FimNaoAnteriorAoInicio_Aceita()
    {
        var inicio = new DateOnly(2026, 11, 10);

        Periodo.Criar(inicio, new DateOnly(2026, 12, 24)).EhSucesso.ShouldBeTrue();
        Periodo.Criar(inicio, inicio).EhSucesso.ShouldBeTrue();
    }

    /// <summary>
    /// Dada data fim anterior à data início, quando o período é criado, então devolve campanha.periodo_invalido.
    /// </summary>
    [Fact]
    public void Periodo_Criar_FimAnteriorAoInicio_DevolveErro()
    {
        var resultado = Periodo.Criar(new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 1));

        resultado.Erro.Codigo.ShouldBe(Periodo.CodigoInvalido);
    }

    /// <summary>
    /// Dado um período, quando se pergunta por um dia, então início e fim contam como dentro.
    /// </summary>
    /// <param name="dia">Dia conferido.</param>
    /// <param name="esperado">Se o dia está dentro do período.</param>
    [Theory]
    [InlineData("2026-11-09", false)]
    [InlineData("2026-11-10", true)]
    [InlineData("2026-12-01", true)]
    [InlineData("2026-12-24", true)]
    [InlineData("2026-12-25", false)]
    public void Periodo_Contem_InicioEFimSaoInclusivos(string dia, bool esperado)
    {
        var periodo = Periodo.Criar(new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 24)).Valor;

        periodo.Contem(DateOnly.Parse(dia, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(esperado);
    }
}
