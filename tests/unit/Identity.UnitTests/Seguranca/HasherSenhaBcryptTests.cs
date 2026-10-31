using Identity.Infrastructure.Seguranca;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes do hash de senha com BCrypt.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HasherSenhaBcryptTests
{
    /// <summary>
    /// Hasher sob teste.
    /// </summary>
    private readonly HasherSenhaBcrypt _hasher = new();

    /// <summary>
    /// Dada uma senha, quando o hash é gerado, então usa BCrypt com work factor 12 e não contém a senha.
    /// </summary>
    [Fact]
    public void Hash_Senha_GeraBcryptComWorkFactor12()
    {
        var hash = _hasher.Hash("Senha@123");

        hash.ShouldStartWith("$2a$12$");
        hash.ShouldNotContain("Senha@123");
        hash.Length.ShouldBeLessThanOrEqualTo(100);
    }

    /// <summary>
    /// Dada a mesma senha, quando o hash é gerado duas vezes, então os hashes diferem pelo sal.
    /// </summary>
    [Fact]
    public void Hash_MesmaSenhaDuasVezes_GeraHashesDiferentes()
    {
        _hasher.Hash("Senha@123").ShouldNotBe(_hasher.Hash("Senha@123"));
    }

    /// <summary>
    /// Dada uma senha vazia, quando o hash é pedido, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Hash_SenhaVazia_LancaExcecao()
    {
        Should.Throw<ArgumentException>(() => _hasher.Hash(string.Empty));
    }

    /// <summary>
    /// Dado um hash gerado, quando a senha correta é verificada, então confere.
    /// </summary>
    [Fact]
    public void Verificar_SenhaCorreta_DevolveVerdadeiro()
    {
        var hash = _hasher.Hash("Senha@123");

        _hasher.Verificar("Senha@123", hash).ShouldBeTrue();
    }

    /// <summary>
    /// Dado um hash gerado, quando uma senha errada é verificada, então não confere.
    /// </summary>
    [Fact]
    public void Verificar_SenhaErrada_DevolveFalso()
    {
        var hash = _hasher.Hash("Senha@123");

        _hasher.Verificar("outra", hash).ShouldBeFalse();
    }

    /// <summary>
    /// Dados valores vazios ou um hash que não é BCrypt, quando verificados, então devolve falso sem lançar exceção.
    /// </summary>
    /// <param name="senha">Senha testada.</param>
    /// <param name="hash">Hash testado.</param>
    [Theory]
    [InlineData("", "$2a$12$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ01234")]
    [InlineData("Senha@123", "")]
    [InlineData("Senha@123", "isto-nao-e-bcrypt")]
    public void Verificar_EntradaInvalida_DevolveFalso(string senha, string hash)
    {
        _hasher.Verificar(senha, hash).ShouldBeFalse();
    }

    /// <summary>
    /// Dado um usuário inexistente, quando a verificação fictícia roda, então devolve falso e gasta tempo de hash real.
    /// </summary>
    [Fact]
    public void VerificarSemUsuario_QualquerSenha_DevolveFalsoComCustoDeHash()
    {
        var inicio = System.Diagnostics.Stopwatch.StartNew();

        _hasher.VerificarSemUsuario("Senha@123").ShouldBeFalse();
        _hasher.VerificarSemUsuario(null!).ShouldBeFalse();

        inicio.Elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(20));
    }
}
