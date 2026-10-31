using Identity.Domain.Usuarios;
using Identity.Infrastructure.Seguranca;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes do hash HMAC do e-mail.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HasherEmailHmacTests
{
    /// <summary>
    /// Dado o mesmo e-mail e a mesma chave, quando o hash é calculado duas vezes, então o resultado é igual e tem 32 bytes.
    /// </summary>
    [Fact]
    public void CalcularHash_MesmoEmailEChave_EhDeterministico()
    {
        var hasher = new HasherEmailHmac("chave-de-teste");
        var email = Email.Criar("maria@exemplo.com").Valor;

        var a = hasher.CalcularHash(email);

        a.Length.ShouldBe(32);
        a.ShouldBe(hasher.CalcularHash(email));
    }

    /// <summary>
    /// Dados e-mails ou chaves diferentes, quando o hash é calculado, então os resultados diferem.
    /// </summary>
    [Fact]
    public void CalcularHash_EmailOuChaveDiferente_GeraHashDiferente()
    {
        var email = Email.Criar("maria@exemplo.com").Valor;
        var outro = Email.Criar("joao@exemplo.com").Valor;
        var hasher = new HasherEmailHmac("chave-de-teste");

        hasher.CalcularHash(email).ShouldNotBe(hasher.CalcularHash(outro));
        hasher.CalcularHash(email).ShouldNotBe(new HasherEmailHmac("outra-chave").CalcularHash(email));
    }

    /// <summary>
    /// Dada uma caixa diferente no texto original, quando o e-mail é normalizado, então o hash é o mesmo.
    /// </summary>
    [Fact]
    public void CalcularHash_EmailEmCaixaDiferente_EhIgual()
    {
        var hasher = new HasherEmailHmac("chave-de-teste");

        hasher.CalcularHash(Email.Criar("Maria@Exemplo.com").Valor)
            .ShouldBe(hasher.CalcularHash(Email.Criar("maria@exemplo.com").Valor));
    }

    /// <summary>
    /// Dados argumentos ausentes, quando usados, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void ArgumentosAusentes_LancaExcecao()
    {
        Should.Throw<ArgumentException>(() => new HasherEmailHmac(" "));
        Should.Throw<ArgumentNullException>(() => new HasherEmailHmac("chave").CalcularHash(null!));
    }
}
