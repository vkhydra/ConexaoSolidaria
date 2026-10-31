using Identity.Domain.Comum;

namespace Identity.Domain.Usuarios;

/// <summary>
/// Raiz do agregado de identidade: quem entra na plataforma e com qual papel.
/// </summary>
public sealed class Usuario
{
    /// <summary>
    /// Código do erro de hash de senha vazio.
    /// </summary>
    public const string CodigoSenhaHashInvalido = "usuario.senha_hash_invalido";

    /// <summary>
    /// Nome gravado para o gestor criado pela carga inicial, que não informa o nome.
    /// </summary>
    public const string NomeDoGestorInicial = "Gestor da ONG";

    /// <summary>
    /// Construtor sem parâmetros usado pelo EF Core para materializar o agregado.
    /// </summary>
    private Usuario()
    {
        NomeCompleto = string.Empty;
        Email = null!;
    }

    /// <summary>
    /// Cria um usuário com todos os dados.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="nomeCompleto">Nome completo.</param>
    /// <param name="email">E-mail normalizado.</param>
    /// <param name="senhaHash">Hash BCrypt da senha.</param>
    /// <param name="role">Papel do usuário.</param>
    /// <param name="criadoEm">Data e hora do cadastro.</param>
    private Usuario(UsuarioId id, string nomeCompleto, Email email, string senhaHash, Role role, DateTimeOffset criadoEm)
    {
        Id = id;
        NomeCompleto = nomeCompleto;
        Email = email;
        SenhaHash = senhaHash;
        Role = role;
        CriadoEm = criadoEm;
    }

    /// <summary>
    /// Obtém o identificador do usuário (claim <c>sub</c> do token).
    /// </summary>
    public UsuarioId Id { get; private set; }

    /// <summary>
    /// Obtém o nome completo; fica como texto fixo depois da anonimização.
    /// </summary>
    public string NomeCompleto { get; private set; }

    /// <summary>
    /// Obtém o e-mail normalizado, único na plataforma.
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// Obtém o hash da senha; nulo depois da anonimização.
    /// </summary>
    public string? SenhaHash { get; private set; }

    /// <summary>
    /// Obtém o papel do usuário.
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Obtém a data e a hora do cadastro.
    /// </summary>
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Obtém a data e a hora da anonimização; nulo enquanto o cadastro está ativo.
    /// </summary>
    public DateTimeOffset? AnonimizadoEm { get; private set; }

    /// <summary>
    /// Obtém um valor que indica se os dados pessoais do usuário já foram anonimizados.
    /// </summary>
    public bool EstaAnonimizado => AnonimizadoEm is not null;

    /// <summary>
    /// Cria o primeiro gestor da ONG, usado só pela carga inicial. Gestores não se cadastram pela API.
    /// </summary>
    /// <param name="email">E-mail do gestor.</param>
    /// <param name="senhaHash">Hash da senha já gerado pelo <see cref="IHasherSenha"/>.</param>
    /// <param name="agora">Data e hora atuais.</param>
    /// <returns>O gestor criado ou o erro <c>usuario.senha_hash_invalido</c>.</returns>
    public static Result<Usuario> CriarGestor(Email email, string senhaHash, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            return Result.Falha<Usuario>(new Erro(CodigoSenhaHashInvalido, "O hash da senha do gestor é obrigatório."));
        }

        return Result.Sucesso(new Usuario(UsuarioId.Novo(), NomeDoGestorInicial, email, senhaHash, Role.GestorONG, agora));
    }

    /// <summary>
    /// Confere a senha informada no login. Usuário anonimizado ou sem senha nunca autentica.
    /// </summary>
    /// <param name="senha">Senha em texto digitada no login.</param>
    /// <param name="hasher">Serviço que compara a senha com o hash.</param>
    /// <returns><c>true</c> se a senha está correta e o cadastro não foi anonimizado.</returns>
    public bool VerificarSenha(string senha, IHasherSenha hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        if (EstaAnonimizado || SenhaHash is null)
        {
            return false;
        }

        return hasher.Verificar(senha, SenhaHash);
    }
}
