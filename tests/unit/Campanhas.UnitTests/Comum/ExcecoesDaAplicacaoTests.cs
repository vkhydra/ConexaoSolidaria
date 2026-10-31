using Campanhas.Application.Comum;

namespace Campanhas.UnitTests.Comum;

/// <summary>
/// Testes dos tipos de apoio da camada de aplicação: página de resultados e exceção de conflito de versão.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExcecoesDaAplicacaoTests
{
    /// <summary>
    /// Dados os construtores padrão, quando usados, então guardam a mensagem e a causa.
    /// </summary>
    [Fact]
    public void ConflitoDeVersaoException_Construtores_GuardamMensagemECausa()
    {
        var causa = new InvalidOperationException("causa");

        new ConflitoDeVersaoException().ShouldNotBeNull();
        new ConflitoDeVersaoException("mensagem").Message.ShouldBe("mensagem");
        new ConflitoDeVersaoException("mensagem", causa).InnerException.ShouldBe(causa);
    }

    /// <summary>
    /// Dada uma página, quando criada, então guarda itens, posição e total.
    /// </summary>
    [Fact]
    public void Pagina_Criada_GuardaItensPosicaoETotal()
    {
        var pagina = new Pagina<string>(["a", "b"], 2, 2, 5);

        pagina.Itens.ShouldBe(["a", "b"]);
        pagina.Numero.ShouldBe(2);
        pagina.TamanhoPagina.ShouldBe(2);
        pagina.Total.ShouldBe(5);
    }
}
