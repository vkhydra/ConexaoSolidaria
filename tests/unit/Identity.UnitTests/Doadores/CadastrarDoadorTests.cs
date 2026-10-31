using FluentValidation.Results;
using Identity.Application.Abstracoes;
using Identity.Application.Comum;
using Identity.Application.Doadores;
using Identity.Domain.Usuarios;
using Identity.UnitTests.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Identity.UnitTests.Doadores;

/// <summary>
/// Testes do caso de uso de cadastro de doador: sucesso, validação (400), e-mail em uso (409) e CPF em uso (409).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CadastrarDoadorTests
{
    /// <summary>
    /// CPF válido usado nos cenários.
    /// </summary>
    private const string _cpf = "529.982.247-25";

    /// <summary>
    /// Hash fictício do CPF.
    /// </summary>
    private static readonly byte[] _cpfHash = [1, 2, 3];

    /// <summary>
    /// Repositório de usuários falso.
    /// </summary>
    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();

    /// <summary>
    /// Cifrador de CPF falso.
    /// </summary>
    private readonly ICifradorCpf _cifrador = Substitute.For<ICifradorCpf>();

    /// <summary>
    /// Hasher de senha falso.
    /// </summary>
    private readonly IHasherSenha _hasher = Substitute.For<IHasherSenha>();

    /// <summary>
    /// Relógio controlado pelo teste.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 10, 14, 3, 22, TimeSpan.FromHours(-3)));

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<CadastrarDoador> _log = new();

    /// <summary>
    /// Caso de uso sob teste.
    /// </summary>
    private readonly CadastrarDoador _cadastrar;

    /// <summary>
    /// Prepara os dublês.
    /// </summary>
    public CadastrarDoadorTests()
    {
        _cifrador.CalcularHash(Arg.Any<Cpf>()).Returns(_cpfHash);
        _hasher.Hash("Senha@123").Returns("hash-bcrypt");
        _cadastrar = new CadastrarDoador(
            _usuarios,
            _cifrador,
            _hasher,
            new CadastrarDoadorRequestValidator(),
            _relogio,
            new CadastrarDoadorOpcoes("2026-10"),
            _log);
    }

    /// <summary>
    /// Dados válidos, quando o visitante se cadastra, então grava o doador com consentimento e devolve o CPF mascarado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_DadosValidos_GravaDoadorEDevolveCpfMascarado()
    {
        var resultado = await _cadastrar.ExecutarAsync(Pedido(), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.NomeCompleto.ShouldBe("Maria Silva");
        resultado.Valor.Email.ShouldBe("maria@exemplo.com");
        resultado.Valor.CpfMascarado.ShouldBe("***.982.247-**");
        resultado.Valor.CriadoEm.ShouldBe(_relogio.GetUtcNow());
        await _usuarios.Received(1).AdicionarAsync(
            Arg.Is<Usuario>(u => u.Role == Role.Doador
                && u.SenhaHash == "hash-bcrypt"
                && u.Consentimento!.Versao == "2026-10"
                && u.Id.Valor == resultado.Valor.Id),
            Arg.Any<CancellationToken>());
        _log.Mensagens.ShouldHaveSingleItem().ShouldBe($"Doador cadastrado: {resultado.Valor.Id}.");
    }

    /// <summary>
    /// Dado um e-mail em outra caixa já cadastrado, quando outro visitante se cadastra, então recebe doador.email_em_uso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_EmailEmUso_DevolveConflito()
    {
        _usuarios.ExisteEmailAsync(Arg.Is<Email>(e => e.Valor == "maria@exemplo.com"), Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _cadastrar.ExecutarAsync(Pedido(email: "MARIA@exemplo.com"), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(CadastrarDoador.CodigoEmailEmUso);
        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um CPF já cadastrado, quando outro visitante o usa com outra máscara, então recebe doador.cpf_em_uso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_CpfEmUso_DevolveConflito()
    {
        _usuarios.ExisteCpfAsync(_cpfHash, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _cadastrar.ExecutarAsync(Pedido(cpf: "52998224725"), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(CadastrarDoador.CodigoCpfEmUso);
        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dados dois cadastros simultâneos, quando o banco recusa o segundo por unicidade, então o conflito vira erro de negócio.
    /// </summary>
    /// <param name="campo">Campo em disputa.</param>
    /// <param name="codigo">Código de erro esperado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(CampoUnico.Email, CadastrarDoador.CodigoEmailEmUso)]
    [InlineData(CampoUnico.Cpf, CadastrarDoador.CodigoCpfEmUso)]
    public async Task ExecutarAsync_CorridaNoBanco_DevolveConflito(CampoUnico campo, string codigo)
    {
        _usuarios.AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UnicidadeVioladaException(campo, new InvalidOperationException())));

        var resultado = await _cadastrar.ExecutarAsync(Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(codigo);
        _log.Mensagens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dados inválidos (CPF, senha, e-mail, nome ou falta de aceite), quando enviados, então devolve validacao com o campo certo e sem repetir o valor.
    /// </summary>
    /// <param name="campo">Campo esperado no erro.</param>
    /// <param name="nome">Nome enviado.</param>
    /// <param name="email">E-mail enviado.</param>
    /// <param name="cpf">CPF enviado.</param>
    /// <param name="senha">Senha enviada.</param>
    /// <param name="aceite">Aceite enviado.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData("cpf", "Maria Silva", "maria@exemplo.com", "529.982.247-26", "Senha@123", true)]
    [InlineData("cpf", "Maria Silva", "maria@exemplo.com", "111.111.111-11", "Senha@123", true)]
    [InlineData("cpf", "Maria Silva", "maria@exemplo.com", "123", "Senha@123", true)]
    [InlineData("cpf", "Maria Silva", "maria@exemplo.com", null, "Senha@123", true)]
    [InlineData("senha", "Maria Silva", "maria@exemplo.com", _cpf, "curta@1", true)]
    [InlineData("senha", "Maria Silva", "maria@exemplo.com", _cpf, "semnumero@", true)]
    [InlineData("senha", "Maria Silva", "maria@exemplo.com", _cpf, "Sem1Especial", true)]
    [InlineData("senha", "Maria Silva", "maria@exemplo.com", _cpf, null, true)]
    [InlineData("email", "Maria Silva", " maria@exemplo.com", _cpf, "Senha@123", true)]
    [InlineData("email", "Maria Silva", null, _cpf, "Senha@123", true)]
    [InlineData("nomeCompleto", "Maria", "maria@exemplo.com", _cpf, "Senha@123", true)]
    [InlineData("nomeCompleto", null, "maria@exemplo.com", _cpf, "Senha@123", true)]
    [InlineData("aceitePoliticaPrivacidade", "Maria Silva", "maria@exemplo.com", _cpf, "Senha@123", false)]
    public async Task ExecutarAsync_DadosInvalidos_DevolveValidacaoPorCampo(string campo, string? nome, string? email, string? cpf, string? senha, bool aceite)
    {
        var request = new CadastrarDoadorRequest(nome, email, cpf, senha, aceite);

        var resultado = await _cadastrar.ExecutarAsync(request, CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(ErrosDeValidacao.CodigoValidacao);
        resultado.Erro.Campos.ShouldNotBeNull();
        resultado.Erro.Campos.Keys.ShouldBe([campo]);
        resultado.Erro.Campos[campo].ShouldAllBe(m => cpf == null || !m.Contains(cpf, StringComparison.Ordinal));
        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
        _hasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    /// <summary>
    /// Dado um pedido sem nenhum campo, quando enviado, então todos os campos aparecem no erro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_PedidoVazio_ListaTodosOsCampos()
    {
        var resultado = await _cadastrar.ExecutarAsync(new CadastrarDoadorRequest(null, null, null, null, false), CancellationToken.None);

        resultado.Erro.Campos!.Keys.ShouldBe(["nomeCompleto", "email", "cpf", "senha", "aceitePoliticaPrivacidade"], ignoreOrder: true);
    }

    /// <summary>
    /// Dada uma versão de política em branco na configuração, quando o visitante se cadastra, então o consentimento é recusado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_VersaoDePoliticaVazia_RecusaConsentimento()
    {
        var cadastrar = new CadastrarDoador(_usuarios, _cifrador, _hasher, new CadastrarDoadorRequestValidator(), _relogio, new CadastrarDoadorOpcoes(" "), _log);

        var resultado = await cadastrar.ExecutarAsync(Pedido(), CancellationToken.None);

        resultado.Erro.Codigo.ShouldBe(Consentimento.CodigoObrigatorio);
    }

    /// <summary>
    /// Dado um pedido nulo, quando enviado, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _cadastrar.ExecutarAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Dado um resultado de validação nulo, quando convertido, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void ErrosDeValidacao_ResultadoNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => ErrosDeValidacao.Converter(null!));
        ErrosDeValidacao.Converter(new ValidationResult([new ValidationFailure(string.Empty, "x")])).Campos.ShouldNotBeNull();
    }

    /// <summary>
    /// Monta um pedido válido, trocando só o que o teste precisa.
    /// </summary>
    /// <param name="email">E-mail do pedido.</param>
    /// <param name="cpf">CPF do pedido.</param>
    /// <returns>O pedido.</returns>
    private static CadastrarDoadorRequest Pedido(string email = "Maria@Exemplo.com", string cpf = _cpf)
    {
        return new CadastrarDoadorRequest("Maria Silva", email, cpf, "Senha@123", true);
    }
}
