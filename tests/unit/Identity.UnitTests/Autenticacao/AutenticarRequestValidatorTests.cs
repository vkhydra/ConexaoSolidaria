using Identity.Application.Autenticacao;

namespace Identity.UnitTests.Autenticacao;

/// <summary>
/// Testes do validador do pedido de login.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AutenticarRequestValidatorTests
{
    /// <summary>
    /// Validador sob teste.
    /// </summary>
    private readonly AutenticarRequestValidator _validador = new();

    /// <summary>
    /// Dado um e-mail e uma senha bem formados, quando validado, então o pedido é aceito.
    /// </summary>
    [Fact]
    public void Validar_DadosValidos_Aceita()
    {
        _validador.Validate(new AutenticarRequest("Maria@Exemplo.com", "Senha@123")).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um e-mail ausente ou em branco, quando validado, então o campo email é recusado.
    /// </summary>
    /// <param name="email">E-mail testado.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validar_EmailAusente_Recusa(string? email)
    {
        var resultado = _validador.Validate(new AutenticarRequest(email, "Senha@123"));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == nameof(AutenticarRequest.Email));
    }

    /// <summary>
    /// Dado um e-mail mal formado, quando validado, então o campo email é recusado.
    /// </summary>
    [Fact]
    public void Validar_EmailMalFormado_Recusa()
    {
        var resultado = _validador.Validate(new AutenticarRequest("sem-arroba", "Senha@123"));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.Single().PropertyName.ShouldBe(nameof(AutenticarRequest.Email));
    }

    /// <summary>
    /// Dada uma senha ausente, quando validado, então o campo senha é recusado.
    /// </summary>
    [Fact]
    public void Validar_SenhaAusente_Recusa()
    {
        var resultado = _validador.Validate(new AutenticarRequest("maria@exemplo.com", null));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.Single().PropertyName.ShouldBe(nameof(AutenticarRequest.Senha));
    }

    /// <summary>
    /// Dada uma senha maior que o limite, quando validado, então o campo senha é recusado.
    /// </summary>
    [Fact]
    public void Validar_SenhaGrandeDemais_Recusa()
    {
        var senha = new string('a', AutenticarRequestValidator.TamanhoMaximoSenha + 1);

        var resultado = _validador.Validate(new AutenticarRequest("maria@exemplo.com", senha));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.Single().PropertyName.ShouldBe(nameof(AutenticarRequest.Senha));
    }
}
