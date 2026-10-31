using Identity.Application.Abstracoes;
using Identity.Domain.Autenticacao;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Persistencia.Repositorios;
using Identity.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

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
            await new UsuarioRepositorio(escrita, banco.Cifrador).AdicionarAsync(gestor, CancellationToken.None);
        }

        await using var leitura = banco.CriarContexto();
        var lido = await new UsuarioRepositorio(leitura, banco.Cifrador).ObterPorEmailAsync(gestor.Email, CancellationToken.None);

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
        var repositorio = new UsuarioRepositorio(contexto, banco.Cifrador);
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

        var lido = await new UsuarioRepositorio(contexto, banco.Cifrador)
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
        var repositorio = new UsuarioRepositorio(contexto, banco.Cifrador);
        await repositorio.AdicionarAsync(NovoGestor(), CancellationToken.None);

        (await repositorio.ExisteGestorAsync(CancellationToken.None)).ShouldBeTrue();
    }

    /// <summary>
    /// Dado um doador gravado, quando o banco é consultado, então o CPF está cifrado (nunca em texto), o hash confere e o consentimento volta.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Doador_AdicionarComCpf_GravaCpfCifradoEHashEConsentimento()
    {
        var cpf = Cpf.Criar("529.982.247-25").Valor;
        var email = Email.Criar($"doador-{Guid.NewGuid():N}@exemplo.com").Valor;
        var consentimento = Consentimento.Criar(true, "2026-10", _agora).Valor;
        var doador = Usuario.CadastrarDoador("Maria Silva", email, cpf, "hash-bcrypt", consentimento, _agora).Valor;
        await using (var escrita = banco.CriarContexto())
        {
            await new UsuarioRepositorio(escrita, banco.Cifrador).AdicionarAsync(doador, CancellationToken.None);
        }

        await using var leitura = banco.CriarContexto();
        var repositorio = new UsuarioRepositorio(leitura, banco.Cifrador);
        var lido = await repositorio.ObterPorIdAsync(doador.Id, CancellationToken.None);

        lido!.Consentimento.ShouldBe(consentimento);
        var cifrado = leitura.Entry(lido).Property<byte[]?>("CpfCifrado").CurrentValue;
        cifrado.ShouldNotBeNull();
        banco.Cifrador.Decifrar(cifrado).ShouldBe(cpf);
        System.Text.Encoding.UTF8.GetString(cifrado).ShouldNotContain("52998224725");
        (await repositorio.ExisteCpfAsync(banco.Cifrador.CalcularHash(cpf), CancellationToken.None)).ShouldBeTrue();
        (await repositorio.ExisteEmailAsync(email, CancellationToken.None)).ShouldBeTrue();
        (await repositorio.ExisteCpfAsync(banco.Cifrador.CalcularHash(Cpf.Criar("123.456.789-09").Valor), CancellationToken.None)).ShouldBeFalse();
    }

    /// <summary>
    /// Dado um CPF já cadastrado, quando outro doador o usa, então o índice único parcial do banco recusa; o gestor, sem CPF, não conflita.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Doador_CpfDuplicado_ViolaIndiceUnicoParcial()
    {
        var cpf = Cpf.Criar("111.444.777-35").Valor;
        var consentimento = Consentimento.Criar(true, "2026-10", _agora).Valor;
        Usuario Novo() => Usuario.CadastrarDoador("Joao Souza", Email.Criar($"d-{Guid.NewGuid():N}@exemplo.com").Valor, cpf, "hash", consentimento, _agora).Valor;
        await using (var a = banco.CriarContexto())
        {
            await new UsuarioRepositorio(a, banco.Cifrador).AdicionarAsync(Novo(), CancellationToken.None);
            await new UsuarioRepositorio(a, banco.Cifrador).AdicionarAsync(NovoGestor(), CancellationToken.None);
            await new UsuarioRepositorio(a, banco.Cifrador).AdicionarAsync(NovoGestor(), CancellationToken.None);
        }

        await using var b = banco.CriarContexto();

        var excecao = await Should.ThrowAsync<UnicidadeVioladaException>(() => new UsuarioRepositorio(b, banco.Cifrador).AdicionarAsync(Novo(), CancellationToken.None));
        excecao.Campo.ShouldBe(CampoUnico.Cpf);
    }

    /// <summary>
    /// Dado um e-mail já cadastrado, quando outro usuário o usa, então o índice único do banco recusa e o repositório sinaliza conflito de e-mail.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_EmailDuplicado_ViolaIndiceUnico()
    {
        var primeiro = NovoGestor();
        var segundo = Usuario.CriarGestor(primeiro.Email, "outro-hash", _agora).Valor;
        await using (var a = banco.CriarContexto())
        {
            await new UsuarioRepositorio(a, banco.Cifrador).AdicionarAsync(primeiro, CancellationToken.None);
        }

        await using var b = banco.CriarContexto();

        var excecao = await Should.ThrowAsync<UnicidadeVioladaException>(() => new UsuarioRepositorio(b, banco.Cifrador).AdicionarAsync(segundo, CancellationToken.None));
        excecao.Campo.ShouldBe(CampoUnico.Email);
    }

    /// <summary>
    /// Dada uma violação de restrição que não é de e-mail nem de CPF (chave primária), quando o usuário é gravado de novo, então a exceção original do banco sobe.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Usuario_ChavePrimariaDuplicada_RelancaExcecaoDoBanco()
    {
        var gestor = NovoGestor();
        await using (var a = banco.CriarContexto())
        {
            await new UsuarioRepositorio(a, banco.Cifrador).AdicionarAsync(gestor, CancellationToken.None);
        }

        await using var b = banco.CriarContexto();

        await Should.ThrowAsync<DbUpdateException>(() => new UsuarioRepositorio(b, banco.Cifrador).AdicionarAsync(gestor, CancellationToken.None));
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
    /// Dadas duas primeiras falhas simultâneas do mesmo e-mail, quando a segunda é salva, então não lança exceção e há uma linha só.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Tentativa_DuasPrimeirasFalhasConcorrentes_SegundaNaoLanca()
    {
        var hash = Guid.NewGuid().ToByteArray();
        await using var a = banco.CriarContexto();
        await using var b = banco.CriarContexto();
        var primeira = TentativaLogin.Nova(hash, _agora);
        primeira.RegistrarFalha(_agora);
        var segunda = TentativaLogin.Nova(hash, _agora);
        segunda.RegistrarFalha(_agora);

        await new TentativaLoginRepositorio(a).SalvarAsync(primeira, CancellationToken.None);
        await Should.NotThrowAsync(() => new TentativaLoginRepositorio(b).SalvarAsync(segunda, CancellationToken.None));

        await using var leitura = banco.CriarContexto();
        (await leitura.TentativasLogin.CountAsync(t => t.EmailHash == hash, TestContext.Current.CancellationToken)).ShouldBe(1);
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
        var usuarios = new UsuarioRepositorio(contexto, banco.Cifrador);
        var tentativas = new TentativaLoginRepositorio(contexto);

        await Should.ThrowAsync<ArgumentNullException>(() => usuarios.ObterPorEmailAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => usuarios.AdicionarAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => tentativas.ObterAsync(null!, CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(() => tentativas.SalvarAsync(null!, CancellationToken.None));
    }
}
