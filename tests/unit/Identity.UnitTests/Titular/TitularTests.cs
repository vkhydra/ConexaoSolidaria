using Identity.Application.Abstracoes;
using Identity.Application.Titular;
using Identity.Domain.Usuarios;
using Microsoft.Extensions.Time.Testing;

namespace Identity.UnitTests.Titular;

/// <summary>
/// Testes dos casos de uso de direitos do titular: acesso aos dados e anonimização.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TitularTests
{
    /// <summary>
    /// Repositório de usuários falso.
    /// </summary>
    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();

    /// <summary>
    /// Relógio controlado pelo teste.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(-3)));

    /// <summary>
    /// Doador usado nos cenários.
    /// </summary>
    private readonly Usuario _doador;

    /// <summary>
    /// Cria um doador e programa o repositório falso para devolvê-lo.
    /// </summary>
    public TitularTests()
    {
        var cpf = Cpf.Criar("529.982.247-25").Valor;
        _doador = Usuario.CadastrarDoador(
            "Maria Silva",
            Email.Criar("maria@exemplo.com").Valor,
            cpf,
            "hash",
            Consentimento.Criar(true, "2026-10", _relogio.GetUtcNow().AddDays(-10)).Valor,
            _relogio.GetUtcNow().AddDays(-10)).Valor;
        _usuarios.ObterPorIdAsync(_doador.Id, Arg.Any<CancellationToken>()).Returns(_doador);
        _usuarios.ObterCpfAsync(_doador.Id, Arg.Any<CancellationToken>()).Returns(cpf);
    }

    /// <summary>
    /// Dado um doador ativo, quando consulta os próprios dados, então vê nome, e-mail, CPF mascarado e o aceite.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ObterMeusDados_DoadorAtivo_DevolveDadosComCpfMascarado()
    {
        var resultado = await new ObterMeusDados(_usuarios).ExecutarAsync(_doador.Id, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.Id.ShouldBe(_doador.Id.Valor);
        resultado.Valor.NomeCompleto.ShouldBe("Maria Silva");
        resultado.Valor.Email.ShouldBe("maria@exemplo.com");
        resultado.Valor.CpfMascarado.ShouldBe("***.982.247-**");
        resultado.Valor.CriadoEm.ShouldBe(_doador.CriadoEm);
        resultado.Valor.Consentimento.ShouldBe(new ConsentimentoResponse("2026-10", _doador.Consentimento!.Em));
    }

    /// <summary>
    /// Dado um token de usuário inexistente, anonimizado, sem consentimento (gestor) ou sem CPF, quando consulta, então não é autenticado.
    /// </summary>
    /// <param name="cenario">Cenário testado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("inexistente")]
    [InlineData("anonimizado")]
    [InlineData("gestor")]
    [InlineData("sem-cpf")]
    public async Task ObterMeusDados_UsuarioNaoElegivel_DevolveNaoAutenticado(string cenario)
    {
        var id = UsuarioId.Novo();
        switch (cenario)
        {
            case "anonimizado":
                _doador.Anonimizar(_relogio.GetUtcNow());
                id = _doador.Id;
                break;
            case "gestor":
                var gestor = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, "hash", _relogio.GetUtcNow()).Valor;
                _usuarios.ObterPorIdAsync(gestor.Id, Arg.Any<CancellationToken>()).Returns(gestor);
                id = gestor.Id;
                break;
            case "sem-cpf":
                _usuarios.ObterCpfAsync(_doador.Id, Arg.Any<CancellationToken>()).Returns((Cpf?)null);
                id = _doador.Id;
                break;
            default:
                break;
        }

        var resultado = await new ObterMeusDados(_usuarios).ExecutarAsync(id, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDoTitular.CodigoNaoAutenticado);
    }

    /// <summary>
    /// Dado um doador ativo, quando solicita a exclusão, então o cadastro é anonimizado e gravado uma vez.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task AnonimizarMeusDados_DoadorAtivo_AnonimizaEGrava()
    {
        var resultado = await new AnonimizarMeusDados(_usuarios, _relogio).ExecutarAsync(_doador.Id, CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        _doador.EstaAnonimizado.ShouldBeTrue();
        _doador.AnonimizadoEm.ShouldBe(_relogio.GetUtcNow());
        await _usuarios.Received(1).SalvarAsync(_doador, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um usuário inexistente ou já anonimizado, quando solicita a exclusão, então não é autenticado e nada é gravado.
    /// </summary>
    /// <param name="jaAnonimizado">Se o usuário já estava anonimizado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonimizarMeusDados_InexistenteOuJaAnonimizado_DevolveNaoAutenticado(bool jaAnonimizado)
    {
        var id = UsuarioId.Novo();
        if (jaAnonimizado)
        {
            _doador.Anonimizar(_relogio.GetUtcNow());
            id = _doador.Id;
        }

        var resultado = await new AnonimizarMeusDados(_usuarios, _relogio).ExecutarAsync(id, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDoTitular.CodigoNaoAutenticado);
        await _usuarios.DidNotReceive().SalvarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }
}
