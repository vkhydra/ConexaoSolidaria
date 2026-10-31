using System.Security.Cryptography;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Seguranca;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes do emissor de JWT RS256: assinatura, validade e payload sem dados pessoais.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmissorJwtRs256Tests : IDisposable
{
    /// <summary>
    /// Chave RSA gerada só para os testes.
    /// </summary>
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>
    /// Relógio controlado pelo teste.
    /// </summary>
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 10, 17, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Opções do token com a chave de teste.
    /// </summary>
    private readonly JwtOpcoes _opcoes;

    /// <summary>
    /// Usuário cujo token é emitido.
    /// </summary>
    private readonly Usuario _usuario = Usuario.CriarGestor(Email.Criar("gestor@exemplo.com").Valor, "hash", DateTimeOffset.UtcNow).Valor;

    /// <summary>
    /// Prepara as opções com a chave privada em PEM.
    /// </summary>
    public EmissorJwtRs256Tests()
    {
        _opcoes = new JwtOpcoes { ChavePrivadaPem = _rsa.ExportRSAPrivateKeyPem() };
    }

    /// <summary>
    /// Dado um usuário, quando o token é emitido, então a assinatura RS256 é válida com a chave pública e o prazo é de 60 minutos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [Fact]
    public async Task Emitir_Usuario_GeraTokenAssinadoComChavePublicaValida()
    {
        using var emissor = new EmissorJwtRs256(_opcoes, _relogio);

        var token = emissor.Emitir(_usuario);

        token.ExpiraEmSegundos.ShouldBe(3600);
        var parametros = new TokenValidationParameters
        {
            IssuerSigningKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)),
            ValidIssuer = "conexao-identity",
            ValidAudience = "conexao-api",
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            LifetimeValidator = (_, expira, _, _) => expira > _relogio.GetUtcNow().UtcDateTime,
        };
        var validacao = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken, parametros);
        validacao.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// Dado um token emitido, quando o payload é lido, então tem só sub, role, jti, iat, exp, iss e aud, sem nome nem e-mail.
    /// </summary>
    [Fact]
    public void Emitir_Usuario_PayloadSemDadosPessoais()
    {
        using var emissor = new EmissorJwtRs256(_opcoes, _relogio);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(emissor.Emitir(_usuario).AccessToken);

        var campos = System.Text.Json.JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(jwt.EncodedPayload)).RootElement.EnumerateObject().Select(p => p.Name);
        campos.ShouldBe(["sub", "role", "jti", "iat", "exp", "iss", "aud"], ignoreOrder: true);
        jwt.Subject.ShouldBe(_usuario.Id.ToString());
        jwt.GetClaim("role").Value.ShouldBe("GestorONG");
        jwt.ValidTo.ShouldBe(_relogio.GetUtcNow().UtcDateTime.AddHours(1));
        jwt.EncodedPayload.ShouldNotContain("exemplo.com");
        jwt.Alg.ShouldBe("RS256");
    }

    /// <summary>
    /// Dados dois tokens do mesmo usuário, quando emitidos, então os identificadores jti são diferentes.
    /// </summary>
    [Fact]
    public void Emitir_DoisTokens_TemJtiDiferentes()
    {
        using var emissor = new EmissorJwtRs256(_opcoes, _relogio);
        var manipulador = new JsonWebTokenHandler();

        var a = manipulador.ReadJsonWebToken(emissor.Emitir(_usuario).AccessToken);
        var b = manipulador.ReadJsonWebToken(emissor.Emitir(_usuario).AccessToken);

        a.Id.ShouldNotBe(b.Id);
    }

    /// <summary>
    /// Dado um usuário nulo, quando o token é pedido, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Emitir_UsuarioNulo_LancaExcecao()
    {
        using var emissor = new EmissorJwtRs256(_opcoes, _relogio);

        Should.Throw<ArgumentNullException>(() => emissor.Emitir(null!));
    }

    /// <summary>
    /// Dada uma chave ausente ou inválida, quando o emissor é criado, então a configuração é recusada.
    /// </summary>
    /// <param name="pem">Conteúdo PEM testado.</param>
    [Theory]
    [InlineData("")]
    [InlineData("isto-nao-e-um-pem")]
    public void Construtor_ChaveAusenteOuInvalida_LancaExcecao(string pem)
    {
        var opcoes = new JwtOpcoes { ChavePrivadaPem = pem };

        Should.Throw<ArgumentException>(() => new EmissorJwtRs256(opcoes, _relogio));
    }

    /// <summary>
    /// Dados argumentos nulos, quando o emissor é criado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Construtor_ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new EmissorJwtRs256(null!, _relogio));
        Should.Throw<ArgumentNullException>(() => new EmissorJwtRs256(_opcoes, null!));
    }

    /// <summary>
    /// Libera a chave RSA de teste.
    /// </summary>
    public void Dispose()
    {
        _rsa.Dispose();
    }
}
