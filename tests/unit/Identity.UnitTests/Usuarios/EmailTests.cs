using Identity.Domain.Usuarios;

namespace Identity.UnitTests.Usuarios;

/// <summary>
/// Testes do value object <see cref="Email"/>: normalização, validação de formato e igualdade.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmailTests
{
    /// <summary>
    /// Dado um e-mail válido com letras maiúsculas, quando criado, então é normalizado em minúsculas.
    /// </summary>
    [Fact]
    public void Criar_EmailValidoComMaiusculas_NormalizaEmMinusculas()
    {
        var resultado = Email.Criar("Maria@Exemplo.COM");

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Valor.ShouldBe("maria@exemplo.com");
        resultado.Valor.ToString().ShouldBe("maria@exemplo.com");
    }

    /// <summary>
    /// Dados e-mails válidos de formatos comuns, quando criados, então são aceitos.
    /// </summary>
    /// <param name="texto">E-mail válido.</param>
    [Theory]
    [InlineData("a@b.co")]
    [InlineData("nome.sobrenome@empresa.com.br")]
    [InlineData("nome+tag@exemplo.org")]
    [InlineData("x_y-z@sub.dominio.net")]
    public void Criar_EmailValido_RetornaSucesso(string texto)
    {
        Email.Criar(texto).EhSucesso.ShouldBeTrue();
    }

    /// <summary>
    /// Dados textos que não são e-mails, quando criados, então retornam o erro de e-mail inválido.
    /// </summary>
    /// <param name="texto">Texto inválido.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba.com")]
    [InlineData("@semlocal.com")]
    [InlineData("semdominio@")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("sem@ponto")]
    [InlineData("com espaco@exemplo.com")]
    [InlineData(" nas.pontas@exemplo.com ")]
    [InlineData("pontos..seguidos@exemplo.com")]
    [InlineData("a@b..com")]
    [InlineData("a@.com")]
    public void Criar_TextoInvalido_RetornaErroDeEmailInvalido(string? texto)
    {
        var resultado = Email.Criar(texto);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Email.CodigoInvalido);
        if (!string.IsNullOrWhiteSpace(texto))
        {
            resultado.Erro.Mensagem.ShouldNotContain(texto.Trim());
        }
    }

    /// <summary>
    /// Dado um e-mail com exatamente o tamanho máximo, quando criado, então é aceito; com um caractere a mais, é recusado.
    /// </summary>
    [Fact]
    public void Criar_NoLimiteDeTamanho_AceitaEDepoisRecusa()
    {
        var dominio = "@exemplo.com";
        var noLimite = new string('a', Email.TamanhoMaximo - dominio.Length) + dominio;
        var acimaDoLimite = "a" + noLimite;

        Email.Criar(noLimite).EhSucesso.ShouldBeTrue();
        Email.Criar(acimaDoLimite).EhFalha.ShouldBeTrue();
    }

    /// <summary>
    /// Dados dois e-mails que diferem só em maiúsculas, quando comparados, então são iguais.
    /// </summary>
    [Fact]
    public void Igualdade_EmailsComDiferencaSoDeCaixa_SaoIguais()
    {
        var primeiro = Email.Criar("Maria@Exemplo.com").Valor;
        var segundo = Email.Criar("maria@exemplo.COM").Valor;

        primeiro.ShouldBe(segundo);
        primeiro.GetHashCode().ShouldBe(segundo.GetHashCode());
    }

    /// <summary>
    /// Dado um resultado de falha, quando o valor é lido, então lança exceção; e o inverso vale para o erro do sucesso.
    /// </summary>
    [Fact]
    public void Result_AcessoAoQueNaoExiste_LancaExcecao()
    {
        var falha = Email.Criar("invalido");
        var sucesso = Email.Criar("ok@exemplo.com");

        Should.Throw<InvalidOperationException>(() => falha.Valor);
        Should.Throw<InvalidOperationException>(() => sucesso.Erro);
    }
}
