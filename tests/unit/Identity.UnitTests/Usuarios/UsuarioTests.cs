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

    /// <summary>
    /// Dado um usuário sem hash de senha, quando a senha é conferida, então nunca autentica nem consulta o hasher.
    /// </summary>
    [Fact]
    public void VerificarSenha_SemHash_DevolveFalso()
    {
        var gestor = Usuario.CriarGestor(Email.Criar("gestor@ong.org").Valor, "hash-bcrypt", _agora).Valor;
        var hasher = Substitute.For<IHasherSenha>();
        hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        typeof(Usuario).GetProperty(nameof(Usuario.SenhaHash))!.SetValue(gestor, null);

        gestor.VerificarSenha("qualquer", hasher).ShouldBeFalse();
        hasher.DidNotReceive().Verificar(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// Dado um doador, quando o cadastro é anonimizado, então nome, e-mail, CPF e senha deixam de identificar e o resto fica.
    /// </summary>
    [Fact]
    public void Anonimizar_Doador_RemoveDadosPessoaisEMantemOResto()
    {
        var doador = NovoDoador();
        var id = doador.Id;
        var consentimento = doador.Consentimento;
        var agora = _agora.AddDays(30);

        doador.Anonimizar(agora);

        doador.NomeCompleto.ShouldBe(Usuario.NomeAnonimizado);
        doador.Email.Valor.ShouldBe($"anonimo+{id.Valor}@invalid");
        doador.Cpf.ShouldBeNull();
        doador.SenhaHash.ShouldBeNull();
        doador.AnonimizadoEm.ShouldBe(agora);
        doador.EstaAnonimizado.ShouldBeTrue();
        doador.Id.ShouldBe(id);
        doador.Role.ShouldBe(Role.Doador);
        doador.CriadoEm.ShouldBe(_agora);
        doador.Consentimento.ShouldBe(consentimento);
    }

    /// <summary>
    /// Dado um cadastro já anonimizado, quando a anonimização é repetida, então nada muda, nem a data.
    /// </summary>
    [Fact]
    public void Anonimizar_Repetido_NaoAlteraNada()
    {
        var doador = NovoDoador();
        doador.Anonimizar(_agora.AddDays(1));

        doador.Anonimizar(_agora.AddDays(2));

        doador.AnonimizadoEm.ShouldBe(_agora.AddDays(1));
        doador.Email.Valor.ShouldStartWith("anonimo+");
    }

    /// <summary>
    /// Dado um cadastro anonimizado, quando a senha antiga é conferida, então nunca autentica nem consulta o hasher.
    /// </summary>
    [Fact]
    public void VerificarSenha_AposAnonimizar_DevolveFalso()
    {
        var doador = NovoDoador();
        var hasher = Substitute.For<IHasherSenha>();
        hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        doador.Anonimizar(_agora);

        doador.VerificarSenha("Senha@123", hasher).ShouldBeFalse();
        hasher.DidNotReceive().Verificar(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// Cria um doador válido para os testes de anonimização.
    /// </summary>
    /// <returns>O doador cadastrado.</returns>
    private static Usuario NovoDoador()
    {
        return Usuario.CadastrarDoador(
            "Maria Silva",
            Email.Criar("maria@exemplo.com").Valor,
            Cpf.Criar("529.982.247-25").Valor,
            "hash-bcrypt",
            Consentimento.Criar(true, "2026-10", _agora).Valor,
            _agora).Valor;
    }

    /// <summary>
    /// Dados válidos, quando o doador é cadastrado, então tem o papel Doador, CPF, consentimento e nome sem espaços nas pontas.
    /// </summary>
    [Fact]
    public void CadastrarDoador_DadosValidos_CriaDoador()
    {
        var email = Email.Criar("Maria@Exemplo.com").Valor;
        var cpf = Cpf.Criar("529.982.247-25").Valor;
        var consentimento = Consentimento.Criar(true, "2026-10", _agora).Valor;

        var resultado = Usuario.CadastrarDoador("  Maria Silva ", email, cpf, "hash-bcrypt", consentimento, _agora);

        resultado.EhSucesso.ShouldBeTrue();
        var doador = resultado.Valor;
        doador.Role.ShouldBe(Role.Doador);
        doador.NomeCompleto.ShouldBe("Maria Silva");
        doador.Email.Valor.ShouldBe("maria@exemplo.com");
        doador.Cpf.ShouldBe(cpf);
        doador.Consentimento.ShouldBe(consentimento);
        doador.SenhaHash.ShouldBe("hash-bcrypt");
        doador.CriadoEm.ShouldBe(_agora);
        doador.EstaAnonimizado.ShouldBeFalse();
    }

    /// <summary>
    /// Dado um nome ausente, curto ou de uma palavra só, quando o doador é cadastrado, então devolve usuario.nome_invalido.
    /// </summary>
    /// <param name="nome">Nome informado.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ma")]
    [InlineData("Maria")]
    [InlineData("   Maria   ")]
    public void CadastrarDoador_NomeInvalido_DevolveErro(string? nome)
    {
        var resultado = Usuario.CadastrarDoador(nome, Email.Criar("a@b.com").Valor, Cpf.Criar("52998224725").Valor, "hash", Consentimento.Criar(true, "1", _agora).Valor, _agora);

        resultado.EhFalha.ShouldBeTrue();
        resultado.Erro.Codigo.ShouldBe(Usuario.CodigoNomeInvalido);
    }

    /// <summary>
    /// Dado um nome com mais de 150 caracteres, quando o doador é cadastrado, então devolve usuario.nome_invalido.
    /// </summary>
    [Fact]
    public void CadastrarDoador_NomeGrandeDemais_DevolveErro()
    {
        var nome = "Maria " + new string('a', Usuario.NomeTamanhoMaximo);

        var resultado = Usuario.CadastrarDoador(nome, Email.Criar("a@b.com").Valor, Cpf.Criar("52998224725").Valor, "hash", Consentimento.Criar(true, "1", _agora).Valor, _agora);

        resultado.Erro.Codigo.ShouldBe(Usuario.CodigoNomeInvalido);
    }

    /// <summary>
    /// Dado um hash de senha vazio, quando o doador é cadastrado, então devolve usuario.senha_hash_invalido.
    /// </summary>
    [Fact]
    public void CadastrarDoador_HashVazio_DevolveErro()
    {
        var resultado = Usuario.CadastrarDoador("Maria Silva", Email.Criar("a@b.com").Valor, Cpf.Criar("52998224725").Valor, " ", Consentimento.Criar(true, "1", _agora).Valor, _agora);

        resultado.Erro.Codigo.ShouldBe(Usuario.CodigoSenhaHashInvalido);
    }

    /// <summary>
    /// Dado um argumento nulo, quando o doador é cadastrado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void CadastrarDoador_ArgumentoNulo_LancaExcecao()
    {
        var email = Email.Criar("a@b.com").Valor;
        var cpf = Cpf.Criar("52998224725").Valor;
        var consentimento = Consentimento.Criar(true, "1", _agora).Valor;

        Should.Throw<ArgumentNullException>(() => Usuario.CadastrarDoador("Maria Silva", null!, cpf, "h", consentimento, _agora));
        Should.Throw<ArgumentNullException>(() => Usuario.CadastrarDoador("Maria Silva", email, null!, "h", consentimento, _agora));
        Should.Throw<ArgumentNullException>(() => Usuario.CadastrarDoador("Maria Silva", email, cpf, "h", null!, _agora));
    }
}
