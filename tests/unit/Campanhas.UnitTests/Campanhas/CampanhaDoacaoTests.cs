using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes das regras de doação do agregado <see cref="Campanha"/>: aceite no período e soma ao total.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CampanhaDoacaoTests
{
    /// <summary>
    /// Primeiro dia da campanha usada nos cenários.
    /// </summary>
    private static readonly DateOnly _inicio = new(2026, 11, 10);

    /// <summary>
    /// Último dia da campanha usada nos cenários.
    /// </summary>
    private static readonly DateOnly _fim = new(2026, 12, 24);

    /// <summary>
    /// Instante da criação usado nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dada uma campanha ativa, quando se pergunta por um dia antes do início, dentro do período ou depois do fim,
    /// então só os dias dentro do período, inclusive os extremos, aceitam doação.
    /// </summary>
    /// <param name="dia">Dia conferido.</param>
    /// <param name="esperado">Se a doação deve ser aceita.</param>
    [Theory]
    [InlineData("2026-11-09", false)]
    [InlineData("2026-11-10", true)]
    [InlineData("2026-12-01", true)]
    [InlineData("2026-12-24", true)]
    [InlineData("2026-12-25", false)]
    public void PodeReceberDoacao_CampanhaAtiva_DependeDoPeriodo(string dia, bool esperado)
    {
        var campanha = NovaCampanha();

        campanha.PodeReceberDoacao(DateOnly.Parse(dia, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(esperado);
    }

    /// <summary>
    /// Dada uma campanha concluída ou cancelada, quando se pergunta por um dia dentro do período, então a doação é recusada.
    /// </summary>
    /// <param name="cancelar">Se a campanha é cancelada (verdadeiro) ou concluída (falso).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PodeReceberDoacao_CampanhaEncerrada_Recusa(bool cancelar)
    {
        var campanha = NovaCampanha();
        _ = cancelar ? campanha.Cancelar(_agora) : campanha.Concluir(_agora);

        campanha.PodeReceberDoacao(new DateOnly(2026, 12, 1)).ShouldBeFalse();
    }

    /// <summary>
    /// Dada uma campanha com total, quando uma doação processada é registrada, então o valor entra no total e a data de alteração muda.
    /// </summary>
    [Fact]
    public void RegistrarDoacaoProcessada_ValorValido_SomaAoTotal()
    {
        var campanha = NovaCampanha();
        var depois = _agora.AddHours(1);

        var primeira = campanha.RegistrarDoacaoProcessada(Dinheiro.Criar(150m).Valor, depois);
        var segunda = campanha.RegistrarDoacaoProcessada(Dinheiro.Criar(49.90m).Valor, depois);

        primeira.EhSucesso.ShouldBeTrue();
        segunda.EhSucesso.ShouldBeTrue();
        campanha.TotalArrecadado.Valor.ShouldBe(199.90m);
        campanha.AtualizadoEm.ShouldBe(depois);
    }

    /// <summary>
    /// Dada uma campanha já encerrada depois do aceite, quando a doação aceita antes é processada, então ainda soma ao total.
    /// </summary>
    [Fact]
    public void RegistrarDoacaoProcessada_CampanhaEncerrada_AindaSoma()
    {
        var campanha = NovaCampanha();
        campanha.Concluir(_agora);

        var resultado = campanha.RegistrarDoacaoProcessada(Dinheiro.Criar(10m).Valor, _agora);

        resultado.EhSucesso.ShouldBeTrue();
        campanha.TotalArrecadado.Valor.ShouldBe(10m);
    }

    /// <summary>
    /// Dado um valor zero, quando a doação processada é registrada, então devolve doacao.valor_invalido e o total não muda.
    /// </summary>
    [Fact]
    public void RegistrarDoacaoProcessada_ValorZero_DevolveErro()
    {
        var campanha = NovaCampanha();

        var resultado = campanha.RegistrarDoacaoProcessada(Dinheiro.Zero, _agora.AddHours(1));

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoDoacaoValorInvalido);
        campanha.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        campanha.AtualizadoEm.ShouldBe(_agora);
    }

    /// <summary>
    /// Dado um valor nulo, quando a doação processada é registrada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void RegistrarDoacaoProcessada_ValorNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => NovaCampanha().RegistrarDoacaoProcessada(null!, _agora));
    }

    /// <summary>
    /// Cria uma campanha ativa de 10000.00 no período dos cenários.
    /// </summary>
    /// <returns>A campanha criada.</returns>
    private static Campanha NovaCampanha()
    {
        return Campanha.Criar("Natal Solidário", "Cestas", Periodo.Criar(_inicio, _fim).Valor, Dinheiro.Criar(10000m).Valor, _inicio, _agora).Valor;
    }
}
