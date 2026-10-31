using Campanhas.Application.Busca;
using ConexaoSolidaria.Contracts.V1;

namespace Campanhas.UnitTests.Busca;

/// <summary>
/// Testes do mapeamento do evento <see cref="CampanhaAlteradaEvent"/> para o documento do índice de busca.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CampanhaIndexadaTests
{
    /// <summary>
    /// Dado um evento de alteração, quando mapeado, então o documento leva os mesmos dados e a versão do evento.
    /// </summary>
    [Fact]
    public void De_Evento_CopiaTodosOsCampos()
    {
        var id = Guid.NewGuid();
        var evento = new CampanhaAlteradaEvent(id, "Páscoa Feliz", "Chocolates para 80 crianças", "Ativa", 5000.00m, new DateOnly(2027, 4, 4), 639000000000000000);

        var documento = CampanhaIndexada.De(evento);

        documento.ShouldBe(new CampanhaIndexada(id, "Páscoa Feliz", "Chocolates para 80 crianças", "Ativa", 5000.00m, new DateOnly(2027, 4, 4), 639000000000000000));
    }

    /// <summary>
    /// Dado um evento nulo, quando mapeado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void De_EventoNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => CampanhaIndexada.De(null!));
    }
}
