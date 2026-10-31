using System.Reflection;
using Identity.Application.Abstracoes;
using Identity.Application.Autenticacao;
using Identity.Domain.Autenticacao;
using Identity.Domain.Usuarios;
using Identity.UnitTests.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Identity.UnitTests.Autenticacao;

/// <summary>
/// Testes do caso de uso de login: sucesso, senha errada, e-mail inexistente, bloqueio e usuário anonimizado.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AutenticarTests
{
    /// <summary>
    /// E-mail do usuário de teste.
    /// </summary>
    private const string _emailTexto = "gestor@exemplo.com";

    /// <summary>
    /// Senha correta do usuário de teste.
    /// </summary>
    private const string _senhaCorreta = "Senha@123";

    /// <summary>
    /// Hash fictício do e-mail.
    /// </summary>
    private static readonly byte[] _emailHash = [9, 8, 7];

    /// <summary>
    /// Repositório de usuários falso.
    /// </summary>
    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();

    /// <summary>
    /// Repositório de tentativas falso.
    /// </summary>
    private readonly ITentativaLoginRepositorio _tentativas = Substitute.For<ITentativaLoginRepositorio>();

    /// <summary>
    /// Hasher de senha falso.
    /// </summary>
    private readonly IHasherSenha _hasherSenha = Substitute.For<IHasherSenha>();

    /// <summary>
    /// Hasher de e-mail falso.
    /// </summary>
    private readonly IHasherEmail _hasherEmail = Substitute.For<IHasherEmail>();

    /// <summary>
    /// Emissor de token falso.
    /// </summary>
    private readonly IEmissorToken _emissor = Substitute.For<IEmissorToken>();

    /// <summary>
    /// Métrica de logins falsa.
    /// </summary>
    private readonly IMetricasLogin _metricas = Substitute.For<IMetricasLogin>();

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<Autenticar> _log = new();

    /// <summary>
    /// Relógio controlado pelo teste.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 10, 14, 0, 0, TimeSpan.FromHours(-3)));

    /// <summary>
    /// Usuário ativo usado nos cenários.
    /// </summary>
    private readonly Usuario _usuario;

    /// <summary>
    /// Caso de uso sob teste.
    /// </summary>
    private readonly Autenticar _autenticar;

    /// <summary>
    /// Prepara os dublês e um gestor cadastrado.
    /// </summary>
    public AutenticarTests()
    {
        var email = Email.Criar(_emailTexto).Valor;
        _usuario = Usuario.CriarGestor(email, "hash-da-senha", _relogio.GetUtcNow()).Valor;

        _hasherEmail.CalcularHash(Arg.Any<Email>()).Returns(_emailHash);
        _hasherSenha.Verificar(_senhaCorreta, "hash-da-senha").Returns(true);
        _usuarios.ObterPorEmailAsync(Arg.Is<Email>(e => e.Valor == _emailTexto), Arg.Any<CancellationToken>()).Returns(_usuario);
        _emissor.Emitir(_usuario).Returns(new TokenEmitido("jwt", 3600));

        _autenticar = new Autenticar(_usuarios, _tentativas, _hasherSenha, _hasherEmail, _emissor, new AutenticarRequestValidator(), _relogio, _metricas, _log);
    }

    /// <summary>
    /// Dadas credenciais corretas, quando o login é feito, então devolve o token e o papel.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_CredenciaisCorretas_DevolveToken()
    {
        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest("Gestor@Exemplo.com", _senhaCorreta), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        resultado.Valor.ShouldBe(new AutenticarResponse("jwt", "Bearer", 3600, "GestorONG"));
        await _tentativas.DidNotReceive().SalvarAsync(Arg.Any<TentativaLogin>(), Arg.Any<CancellationToken>());
        _metricas.Received(1).Registrar(ResultadoLogin.Sucesso);
        _log.Mensagens.ShouldHaveSingleItem().ShouldBe($"Login realizado: {_usuario.Id}.");
        _log.Mensagens.ShouldAllBe(m => !m.Contains("exemplo.com", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Dado um e-mail com falhas anteriores, quando o login dá certo, então a contagem é zerada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_SucessoAposFalhas_ZeraContagem()
    {
        var anterior = TentativaLogin.Nova(_emailHash, _relogio.GetUtcNow());
        anterior.RegistrarFalha(_relogio.GetUtcNow());
        _tentativas.ObterAsync(_emailHash, Arg.Any<CancellationToken>()).Returns(anterior);

        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(_emailTexto, _senhaCorreta), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        anterior.Falhas.ShouldBe(0);
        await _tentativas.Received(1).SalvarAsync(anterior, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma senha errada, quando o login é feito, então recusa e registra a falha.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_SenhaErrada_RecusaERegistraFalha()
    {
        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(_emailTexto, "errada"), CancellationToken.None);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Autenticar.CodigoCredenciaisInvalidas);
        resultado.Erro.Mensagem.ShouldBe("E-mail ou senha inválidos");
        await _tentativas.Received(1).SalvarAsync(
            Arg.Is<TentativaLogin>(t => t.Falhas == 1 && t.EmailHash == _emailHash),
            Arg.Any<CancellationToken>());
        _emissor.DidNotReceive().Emitir(Arg.Any<Usuario>());
        _metricas.Received(1).Registrar(ResultadoLogin.Falha);
        _log.Niveis.ShouldBe([Microsoft.Extensions.Logging.LogLevel.Warning]);
        _log.Mensagens.ShouldAllBe(m => !m.Contains("exemplo.com", StringComparison.OrdinalIgnoreCase) && !m.Contains("errada", StringComparison.Ordinal));
    }

    /// <summary>
    /// Dado um e-mail inexistente, quando o login é feito, então recusa com a mesma mensagem e gasta o mesmo tempo de hash.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_EmailInexistente_RecusaComMesmaMensagemEHashDeTempoConstante()
    {
        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest("ninguem@exemplo.com", _senhaCorreta), CancellationToken.None);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Autenticar.CodigoCredenciaisInvalidas);
        resultado.Erro.Mensagem.ShouldBe("E-mail ou senha inválidos");
        _hasherSenha.Received(1).VerificarSemUsuario(_senhaCorreta);
        await _tentativas.Received(1).SalvarAsync(Arg.Any<TentativaLogin>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um usuário anonimizado, quando o login é feito com a senha antiga, então recusa como credencial inválida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_UsuarioAnonimizado_Recusa()
    {
        typeof(Usuario).GetProperty(nameof(Usuario.AnonimizadoEm), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(_usuario, _relogio.GetUtcNow());

        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(_emailTexto, _senhaCorreta), CancellationToken.None);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Autenticar.CodigoCredenciaisInvalidas);
        _hasherSenha.Received(1).VerificarSemUsuario(_senhaCorreta);
        _emissor.DidNotReceive().Emitir(Arg.Any<Usuario>());
    }

    /// <summary>
    /// Dado um e-mail bloqueado, quando o login é feito com a senha correta, então responde bloqueio sem consultar o usuário.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_EmailBloqueado_RecusaComBloqueio()
    {
        var bloqueada = TentativaLogin.Nova(_emailHash, _relogio.GetUtcNow());
        for (var i = 0; i < TentativaLogin.LimiteDeFalhas; i++)
        {
            bloqueada.RegistrarFalha(_relogio.GetUtcNow());
        }

        _tentativas.ObterAsync(_emailHash, Arg.Any<CancellationToken>()).Returns(bloqueada);

        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(_emailTexto, _senhaCorreta), CancellationToken.None);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Autenticar.CodigoBloqueado);
        await _usuarios.DidNotReceive().ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
        _metricas.Received(1).Registrar(ResultadoLogin.Bloqueado);
        _log.Mensagens.ShouldAllBe(m => !m.Contains("exemplo.com", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Dado um bloqueio que já terminou, quando o login é feito com a senha correta, então entra.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_BloqueioExpirado_Autentica()
    {
        var bloqueada = TentativaLogin.Nova(_emailHash, _relogio.GetUtcNow());
        for (var i = 0; i < TentativaLogin.LimiteDeFalhas; i++)
        {
            bloqueada.RegistrarFalha(_relogio.GetUtcNow());
        }

        _tentativas.ObterAsync(_emailHash, Arg.Any<CancellationToken>()).Returns(bloqueada);
        _relogio.Advance(TentativaLogin.Bloqueio + TimeSpan.FromSeconds(1));

        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(_emailTexto, _senhaCorreta), CancellationToken.None);

        resultado.EhSucesso.ShouldBeTrue();
        bloqueada.Falhas.ShouldBe(0);
    }

    /// <summary>
    /// Dados e-mail e senha em branco, quando o login é feito, então devolve erro de validação por campo sem consultar o banco.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_DadosInvalidos_DevolveErroDeValidacaoPorCampo()
    {
        var resultado = await _autenticar.ExecutarAsync(new AutenticarRequest(string.Empty, string.Empty), CancellationToken.None);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Autenticar.CodigoValidacao);
        resultado.Erro.Campos.ShouldNotBeNull();
        resultado.Erro.Campos.Keys.ShouldBe(["email", "senha"], ignoreOrder: true);
        _metricas.DidNotReceive().Registrar(Arg.Any<ResultadoLogin>());
        await _usuarios.DidNotReceive().ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um pedido nulo, quando o login é feito, então a chamada é recusada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task ExecutarAsync_PedidoNulo_LancaExcecao()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _autenticar.ExecutarAsync(null!, CancellationToken.None));
    }
}
