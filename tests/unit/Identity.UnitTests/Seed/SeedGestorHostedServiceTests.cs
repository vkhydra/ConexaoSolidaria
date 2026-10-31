using Identity.Application.Abstracoes;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Seed;
using Identity.UnitTests.Suporte;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Identity.UnitTests.Seed;

/// <summary>
/// Testes do seed do gestor inicial: criação, idempotência, configuração ausente e falhas sem vazar dados pessoais.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SeedGestorHostedServiceTests
{
    /// <summary>
    /// E-mail do gestor configurado nos testes.
    /// </summary>
    private const string _email = "Gestor@Exemplo.com";

    /// <summary>
    /// Senha do gestor configurada nos testes.
    /// </summary>
    private const string _senha = "SenhaSecreta@2026";

    /// <summary>
    /// Repositório de usuários falso.
    /// </summary>
    private readonly IUsuarioRepositorio _usuarios = Substitute.For<IUsuarioRepositorio>();

    /// <summary>
    /// Hasher de senha falso.
    /// </summary>
    private readonly IHasherSenha _hasher = Substitute.For<IHasherSenha>();

    /// <summary>
    /// Logger que guarda as mensagens para conferência.
    /// </summary>
    private readonly LoggerCaptura<SeedGestorHostedService> _log = new();

    /// <summary>
    /// Prepara o hasher falso.
    /// </summary>
    public SeedGestorHostedServiceTests()
    {
        _hasher.Hash(_senha).Returns("hash-bcrypt");
    }

    /// <summary>
    /// Dado um banco sem gestor, quando o serviço inicia, então cria um gestor com e-mail em minúsculas e senha em hash.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StartAsync_SemGestor_CriaGestor()
    {
        var servico = CriarServico(_email, _senha);

        await servico.StartAsync(CancellationToken.None);

        await _usuarios.Received(1).AdicionarAsync(
            Arg.Is<Usuario>(u => u.Email.Valor == "gestor@exemplo.com" && u.Role == Role.GestorONG && u.SenhaHash == "hash-bcrypt"),
            Arg.Any<CancellationToken>());
        _log.Mensagens.ShouldContain(m => m.StartsWith("Gestor inicial criado: ", StringComparison.Ordinal));
        _log.Mensagens.ShouldAllBe(m => !m.Contains("exemplo.com", StringComparison.OrdinalIgnoreCase) && !m.Contains(_senha, StringComparison.Ordinal));
    }

    /// <summary>
    /// Dado um gestor já existente, quando o serviço inicia de novo, então não cria outro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StartAsync_ComGestorExistente_NaoCriaOutro()
    {
        var existe = false;
        _usuarios.ExisteGestorAsync(Arg.Any<CancellationToken>()).Returns(_ => existe);
        _usuarios.When(u => u.AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>())).Do(_ => existe = true);
        var servico = CriarServico(_email, _senha);

        await servico.StartAsync(CancellationToken.None);
        await servico.StartAsync(CancellationToken.None);

        await _usuarios.Received(1).AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dadas variáveis ausentes ou em branco, quando o serviço inicia, então só registra um aviso.
    /// </summary>
    /// <param name="email">E-mail configurado.</param>
    /// <param name="senha">Senha configurada.</param>
    /// <returns>Tarefa do teste.</returns>
    [Theory]
    [InlineData(null, null)]
    [InlineData(_email, null)]
    [InlineData(null, _senha)]
    [InlineData(" ", _senha)]
    public async Task StartAsync_VariaveisAusentes_RegistraAvisoENaoCria(string? email, string? senha)
    {
        var servico = CriarServico(email, senha);

        await servico.StartAsync(CancellationToken.None);

        _log.Niveis.ShouldContain(LogLevel.Warning);
        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um e-mail inválido, quando o serviço inicia, então avisa sem escrever o valor e não cria o gestor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StartAsync_EmailInvalido_AvisaSemVazarValor()
    {
        var servico = CriarServico("isto-nao-e-email", _senha);

        await servico.StartAsync(CancellationToken.None);

        _log.Niveis.ShouldContain(LogLevel.Warning);
        _log.Mensagens.ShouldAllBe(m => !m.Contains("isto-nao-e-email", StringComparison.Ordinal));
        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dado um banco que falha, quando o serviço inicia, então a falha vira log de erro sem e-mail nem senha e a API continua.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StartAsync_BancoFalha_RegistraErroSemPropagar()
    {
        _usuarios.ExisteGestorAsync(Arg.Any<CancellationToken>()).Returns<bool>(_ => throw new InvalidOperationException("Password=segredo"));
        var servico = CriarServico(_email, _senha);

        await servico.StartAsync(CancellationToken.None);

        _log.Niveis.ShouldContain(LogLevel.Error);
        _log.Mensagens.ShouldAllBe(m => !m.Contains("segredo", StringComparison.Ordinal) && !m.Contains("exemplo.com", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Dado um cancelamento durante o seed, quando o serviço inicia, então o cancelamento é propagado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StartAsync_Cancelado_PropagaCancelamento()
    {
        _usuarios.ExisteGestorAsync(Arg.Any<CancellationToken>()).Returns<bool>(_ => throw new OperationCanceledException());
        var servico = CriarServico(_email, _senha);

        await Should.ThrowAsync<OperationCanceledException>(() => servico.StartAsync(CancellationToken.None));
    }

    /// <summary>
    /// Dado o serviço, quando é parado, então termina sem fazer nada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task StopAsync_Para_TerminaSemEfeito()
    {
        var servico = CriarServico(_email, _senha);

        await servico.StopAsync(CancellationToken.None);

        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Monta o serviço com a configuração informada e os dublês.
    /// </summary>
    /// <param name="email">Valor de <c>Seed:GestorEmail</c>.</param>
    /// <param name="senha">Valor de <c>Seed:GestorSenha</c>.</param>
    /// <returns>Serviço pronto para iniciar.</returns>
    private SeedGestorHostedService CriarServico(string? email, string? senha)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:GestorEmail"] = email, ["Seed:GestorSenha"] = senha })
            .Build();
        var provedor = new ServiceCollection()
            .AddSingleton(_usuarios)
            .AddSingleton(_hasher)
            .BuildServiceProvider();

        return new SeedGestorHostedService(
            provedor.GetRequiredService<IServiceScopeFactory>(),
            configuracao,
            new FakeTimeProvider(),
            _log);
    }
}
