using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Comum;

/// <summary>
/// Testes de <see cref="Result"/>, <see cref="Result{T}"/> e <see cref="Erro"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ResultTests
{
    /// <summary>
    /// Dado o resultado de sucesso sem valor, quando consultado, então é sucesso e não tem erro.
    /// </summary>
    [Fact]
    public void Ok_ResultadoSemValor_EhSucessoSemErro()
    {
        var resultado = Result.Ok();

        resultado.EhSucesso.ShouldBeTrue();
        resultado.EhFalha.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => resultado.Erro);
    }

    /// <summary>
    /// Dado um erro, quando o resultado de falha sem valor é criado, então guarda o erro.
    /// </summary>
    [Fact]
    public void Falha_ComErro_GuardaErro()
    {
        var erro = new Erro("codigo.teste", "Mensagem.");

        var resultado = Result.Falha(erro);

        resultado.EhFalha.ShouldBeTrue();
        resultado.EhSucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe(erro);
    }

    /// <summary>
    /// Dado um erro nulo, quando o resultado de falha é criado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Falha_ErroNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => Result.Falha(null!));
        Should.Throw<ArgumentNullException>(() => Result.Falha<int>(null!));
    }

    /// <summary>
    /// Dado um valor, quando o resultado de sucesso é criado, então expõe o valor e não tem erro.
    /// </summary>
    [Fact]
    public void Sucesso_ComValor_ExpoeValor()
    {
        var resultado = Result.Sucesso(42);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.EhFalha.ShouldBeFalse();
        resultado.Valor.ShouldBe(42);
        Should.Throw<InvalidOperationException>(() => resultado.Erro);
    }

    /// <summary>
    /// Dado um resultado de falha com valor, quando o valor é lido, então a leitura é recusada.
    /// </summary>
    [Fact]
    public void Falha_DeResultadoComValor_NaoTemValor()
    {
        var erro = new Erro("codigo.teste", "Mensagem.");

        var resultado = Result.Falha<int>(erro);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.ShouldBe(erro);
        Should.Throw<InvalidOperationException>(() => resultado.Valor);
    }

    /// <summary>
    /// Dado um erro sem campos, quando criado, então Campos é nulo; com campos, guarda o dicionário.
    /// </summary>
    [Fact]
    public void Erro_Campos_SaoOpcionais()
    {
        new Erro("a", "b").Campos.ShouldBeNull();

        var campos = new Dictionary<string, string[]> { ["email"] = ["inválido"] };
        new Erro("a", "b") { Campos = campos }.Campos.ShouldBe(campos);
    }
}
