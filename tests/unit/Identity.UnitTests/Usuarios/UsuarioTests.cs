using Identity.Domain.Usuarios;

namespace Identity.UnitTests.Usuarios;

/// <summary>
/// Testes do agregado <see cref="Usuario"/>: criação do gestor e verificação de senha.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UsuarioTests
{
    /// <summary>
    /// Instante fixo usado nos testes.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dados válidos, quando o gestor é criado, então ele tem o papel de gestor, nome fixo e cadastro ativo.
    /// </summary>
    [Fact]
    public void CriarGestor_DadosValidos_CriaGestorAtivo()
    {
        var email = Email.Criar("gestor@ong.org").Valor;

        var resultado = Usuario.CriarGestor(email, "hash-bcrypt", _agora);

        resultado.EhSucesso.ShouldBeTrue();
        var gestor = resultado.Valor;
        gestor.Role.ShouldBe(Role.GestorONG);
        gestor.Email.ShouldBe(email);
        gestor.NomeCompleto.ShouldBe(Usuario.NomeDoGestorInicial);
        gestor.SenhaHash.ShouldBe("hash-bcrypt");
        gestor.CriadoEm.ShouldBe(_agora);
        gestor.EstaAnonimizado.ShouldBeFalse();
        gestor.AnonimizadoEm.ShouldBeNull();
        gestor.Id.Valor.ShouldNotBe(Guid.Empty);
    }

    /// <summary>
    /// Dados dois gestores, quando criados em sequência, então os identificadores são diferentes e ordenáveis.
    /// </summary>
    [Fact]
    public void CriarGestor_DoisGestores_TemIdentificadoresDiferentes()
    {
        var email = Email.Criar("gestor@ong.org").Valor;

        var primeiro = Usuario.CriarGestor(email, "h1", _agora).Valor;
        var segundo = Usuario.CriarGestor(email, "h2", _agora).Valor;

        primeiro.Id.ShouldNotBe(segundo.Id);
        primeiro.Id.ToString().ShouldBe(primeiro.Id.Valor.ToString());
    }

    /// <summary>
    /// Dado um hash vazio ou em branco, quando o gestor é criado, então retorna o erro de hash inválido.
    /// </summary>
    /// <param name="hash">Hash inválido.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarGestor_HashVazio_RetornaErro(string hash)
    {
        var resultado = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, hash, _agora);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Usuario.CodigoSenhaHashInvalido);
    }

    /// <summary>
    /// Dado um e-mail nulo, quando o gestor é criado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void CriarGestor_EmailNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => Usuario.CriarGestor(null!, "hash", _agora));
    }

    /// <summary>
    /// Dada a senha correta, quando verificada, então o hasher é consultado com o hash do usuário e autentica.
    /// </summary>
    [Fact]
    public void VerificarSenha_SenhaCorreta_RetornaVerdadeiro()
    {
        var gestor = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, "hash-do-gestor", _agora).Valor;
        var hasher = Substitute.For<IHasherSenha>();
        hasher.Verificar("Senha@123", "hash-do-gestor").Returns(true);

        gestor.VerificarSenha("Senha@123", hasher).ShouldBeTrue();

        hasher.Received(1).Verificar("Senha@123", "hash-do-gestor");
    }

    /// <summary>
    /// Dada uma senha errada, quando verificada, então não autentica.
    /// </summary>
    [Fact]
    public void VerificarSenha_SenhaErrada_RetornaFalso()
    {
        var gestor = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, "hash-do-gestor", _agora).Valor;
        var hasher = Substitute.For<IHasherSenha>();
        hasher.Verificar("errada", "hash-do-gestor").Returns(false);

        gestor.VerificarSenha("errada", hasher).ShouldBeFalse();
    }

    /// <summary>
    /// Dado um hasher nulo, quando a senha é verificada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void VerificarSenha_HasherNulo_LancaExcecao()
    {
        var gestor = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, "hash", _agora).Valor;

        Should.Throw<ArgumentNullException>(() => gestor.VerificarSenha("senha", null!));
    }
}
