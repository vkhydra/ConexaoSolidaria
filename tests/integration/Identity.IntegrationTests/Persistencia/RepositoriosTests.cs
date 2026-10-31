using Identity.Domain.Autenticacao;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia.Repositorios;
using Identity.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Identity.IntegrationTests.Persistencia;

/// <summary>
/// Testes dos repositórios de usuário e de tentativas de login contra um PostgreSQL real (Testcontainers).
/// </summary>
/// <param name="banco">Banco compartilhado pelos testes da classe.</param>
[Trait("Category", "Integration")]
public sealed class RepositoriosTests(BancoIdentityFixture banco) : IClassFixture<BancoIdentityFixture>
{
    /// <summary>
    /// Instante fixo usado nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 14, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Cria um gestor com e-mail único para o teste não colidir com os outros.
    /// </summary>
    /// <returns>O gestor, ainda não gravado.</returns>
    private static Usuario NovoGestor()
    {
        var email = Email.Criar($"gestor-{Guid.NewGuid():N}@exemplo.com").Valor;
        return Usuario.CriarGestor(email, "hash-bcrypt", _agora).Valor;
    }

    /// <summary>
    /// Dado um usuário gravado, quando buscado por e-mail em outra sessão, então volta com os mesmos dados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_AdicionarEObterPorEmail_PersisteEDevolve()
    {
        var gestor = NovoGestor();
        await using (var escrita = banco.CriarContexto())
        {
            await new UsuarioRepositorio(escrita).AdicionarAsync(gestor, CancellationToken.None);
        }

        await using var leitura = banco.CriarContexto();
        var lido = await new UsuarioRepositorio(leitura).ObterPorEmailAsync(gestor.Email, CancellationToken.None);

        lido.ShouldNotBeNull();
        lido.Id.ShouldBe(gestor.Id);
        lido.Role.ShouldBe(Role.GestorONG);
        lido.SenhaHash.ShouldBe("hash-bcrypt");
        lido.CriadoEm.ShouldBe(_agora);
        lido.AnonimizadoEm.ShouldBeNull();
    }

    /// <summary>
    /// Dado um usuário gravado, quando buscado pelo identificador, então volta; um identificador desconhecido devolve nulo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_ObterPorId_DevolveOuNulo()
    {
        var gestor = NovoGestor();
        await using var contexto = banco.CriarContexto();
        var repositorio = new UsuarioRepositorio(contexto);
        await repositorio.AdicionarAsync(gestor, CancellationToken.None);

        (await repositorio.ObterPorIdAsync(gestor.Id, CancellationToken.None)).ShouldNotBeNull();
        (await repositorio.ObterPorIdAsync(UsuarioId.Novo(), CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>
    /// Dado um e-mail inexistente, quando buscado, então devolve nulo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_ObterPorEmailInexistente_DevolveNulo()
    {
        await using var contexto = banco.CriarContexto();

        var lido = await new UsuarioRepositorio(contexto)
            .ObterPorEmailAsync(Email.Criar("ninguem@exemplo.com").Valor, CancellationToken.None);

        lido.ShouldBeNull();
    }

    /// <summary>
    /// Dado um gestor gravado, quando se pergunta se existe gestor, então responde verdadeiro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_ExisteGestor_ComGestorGravado_DevolveVerdadeiro()
    {
        await using var contexto = banco.CriarContexto();
        var repositorio = new UsuarioRepositorio(contexto);
        await repositorio.AdicionarAsync(NovoGestor(), CancellationToken.None);

        (await repositorio.ExisteGestorAsync(CancellationToken.None)).ShouldBeTrue();
    }

    /// <summary>
    /// Dado um e-mail já cadastrado, quando outro usuário o usa, então o índice único do banco recusa.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_EmailDuplicado_ViolaIndiceUnico()
    {
        var primeiro = NovoGestor();
        var segundo = Usuario.CriarGestor(primeiro.Email, "outro-hash", _agora).Valor;
        await using (var a = banco.CriarContexto())
        {
            await new UsuarioRepositorio(a).AdicionarAsync(primeiro, CancellationToken.None);
        }

        await using var b = banco.CriarContexto();

        var excecao = await Should.ThrowAsync<DbUpdateException>(() => new UsuarioRepositorio(b).AdicionarAsync(segundo, CancellationToken.None));
        ((PostgresException)excecao.InnerException!).SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }

    /// <summary>
    /// Dado um e-mail sem falhas, quando o controle é salvo e relido, então o estado persiste e uma nova falha atualiza a mesma linha.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Tentativa_SalvarERelerEAtualizar_Persiste()
    {
        var hash = Guid.NewGuid().ToByteArray();
        await using (var primeira = banco.CriarContexto())
        {
            var repositorio = new TentativaLoginRepositorio(primeira);
            (await repositorio.ObterAsync(hash, CancellationToken.None)).ShouldBeNull();

            var tentativa = TentativaLogin.Nova(hash, _agora);
            tentativa.RegistrarFalha(_agora);
            await repositorio.SalvarAsync(tentativa, CancellationToken.None);
        }

        await using (var segunda = banco.CriarContexto())
        {
            var repositorio = new TentativaLoginRepositorio(segunda);
            var lida = await repositorio.ObterAsync(hash, CancellationToken.None);

            lida.ShouldNotBeNull();
            lida.Falhas.ShouldBe(1);
            lida.RegistrarFalha(_agora.AddMinutes(1));
            await repositorio.SalvarAsync(lida, CancellationToken.None);
        }

        await using var terceira = banco.CriarContexto();
        var final = await new TentativaLoginRepositorio(terceira).ObterAsync(hash, CancellationToken.None);
        final!.Falhas.ShouldBe(2);
        (await terceira.TentativasLogin.CountAsync(t => t.EmailHash == hash, TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    /// <summary>
    /// Dado um controle bloqueado, quando salvo e relido, então o fim do bloqueio persiste.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Tentativa_Bloqueada_PersisteBloqueadoAte()
    {
        var hash = Guid.NewGuid().ToByteArray();
        await using (var escrita = banco.CriarContexto())
        {
            var tentativa = TentativaLogin.Nova(hash, _agora);
            for (var i = 0; i < TentativaLogin.LimiteDeFalhas; i++)
            {
                tentativa.RegistrarFalha(_agora);
            }

            await new TentativaLoginRepositorio(escrita).SalvarAsync(tentativa, CancellationToken.None);
        }

        await using var leitura = banco.CriarContexto();
        var lida = await new TentativaLoginRepositorio(leitura).ObterAsync(hash, CancellationToken.None);

        lida!.BloqueadoAte.ShouldBe(_agora + TentativaLogin.Bloqueio);
        lida.EstaBloqueado(_agora).ShouldBeTrue();
    }

    /// <summary>
    /// Dados argumentos nulos, quando os repositórios são chamados, então recusam.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Repositorios_ArgumentosNulos_LancaExcecao()
    {
        await using var contexto = banco.CriarContexto();
        var usuarios = new UsuarioRepositorio(contexto);
        var tentativas = new TentativaLoginRepositorio(contexto);

        await Should.ThrowAsync<ArgumentNullException>(() => usuarios.ObterPorEmailAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => usuarios.AdicionarAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => tentativas.ObterAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => tentativas.SalvarAsync(null!, CancellationToken.None));
    }
}
