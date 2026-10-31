using System.Security.Cryptography;
using Identity.Domain.Usuarios;
using Identity.Infrastructure.Seguranca;

namespace Identity.UnitTests.Seguranca;

/// <summary>
/// Testes do cifrador de CPF (AES-256-GCM) e do hash HMAC: ida e volta, formato, adulteração e chaves inválidas.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CifradorCpfAesGcmTests
{
    /// <summary>
    /// CPF válido usado nos cenários.
    /// </summary>
    private static readonly Cpf _cpf = Cpf.Criar("529.982.247-25").Valor;

    /// <summary>
    /// Cifrador sob teste, com chaves aleatórias.
    /// </summary>
    private readonly CifradorCpfAesGcm _cifrador = new(Opcoes());

    /// <summary>
    /// Dado um CPF, quando cifrado e decifrado, então volta igual.
    /// </summary>
    [Fact]
    public void CifrarEDecifrar_Cpf_FazIdaEVolta()
    {
        var cifrado = _cifrador.Cifrar(_cpf);

        _cifrador.Decifrar(cifrado).ShouldBe(_cpf);
    }

    /// <summary>
    /// Dado um CPF cifrado, quando inspecionado, então tem o formato versão, nonce, tag e cifra, sem o CPF em texto.
    /// </summary>
    [Fact]
    public void Cifrar_Cpf_UsaFormatoDocumentadoSemTextoPuro()
    {
        var cifrado = _cifrador.Cifrar(_cpf);

        cifrado[0].ShouldBe((byte)1);
        cifrado.Length.ShouldBe(1 + 12 + 16 + 11);
        System.Text.Encoding.UTF8.GetString(cifrado).ShouldNotContain("52998224725");
    }

    /// <summary>
    /// Dado o mesmo CPF, quando cifrado duas vezes, então os resultados diferem (nonce aleatório).
    /// </summary>
    [Fact]
    public void Cifrar_MesmoCpfDuasVezes_GeraResultadosDiferentes()
    {
        _cifrador.Cifrar(_cpf).ShouldNotBe(_cifrador.Cifrar(_cpf));
    }

    /// <summary>
    /// Dado um CPF cifrado adulterado (cifra, tag ou versão), quando decifrado, então falha.
    /// </summary>
    /// <param name="posicao">Posição do byte alterado.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(29)]
    public void Decifrar_Adulterado_LancaExcecao(int posicao)
    {
        var cifrado = _cifrador.Cifrar(_cpf);
        cifrado[posicao] ^= 0x01;

        Should.Throw<CryptographicException>(() => _cifrador.Decifrar(cifrado));
    }

    /// <summary>
    /// Dado um cifrado curto demais, quando decifrado, então falha.
    /// </summary>
    [Fact]
    public void Decifrar_CurtoDemais_LancaExcecao()
    {
        Should.Throw<CryptographicException>(() => _cifrador.Decifrar([1, 2, 3]));
    }

    /// <summary>
    /// Dado um cifrado com outra chave, quando decifrado, então falha.
    /// </summary>
    [Fact]
    public void Decifrar_ComOutraChave_LancaExcecao()
    {
        var cifrado = _cifrador.Cifrar(_cpf);

        Should.Throw<CryptographicException>(() => new CifradorCpfAesGcm(Opcoes()).Decifrar(cifrado));
    }

    /// <summary>
    /// Dado um conteúdo autenticado que não é CPF, quando decifrado, então falha.
    /// </summary>
    [Fact]
    public void Decifrar_ConteudoQueNaoECpf_LancaExcecao()
    {
        var opcoes = Opcoes();
        var cifrador = new CifradorCpfAesGcm(opcoes);
        var texto = System.Text.Encoding.UTF8.GetBytes("abcdefghijk");
        var cifrado = new byte[1 + 12 + 16 + texto.Length];
        cifrado[0] = opcoes.VersaoChave;
        RandomNumberGenerator.Fill(cifrado.AsSpan(1, 12));
        using var aes = new AesGcm(Convert.FromBase64String(opcoes.ChaveCifra), 16);
        aes.Encrypt(cifrado.AsSpan(1, 12), texto, cifrado.AsSpan(29), cifrado.AsSpan(13, 16), cifrado.AsSpan(0, 1));

        Should.Throw<CryptographicException>(() => cifrador.Decifrar(cifrado));
    }

    /// <summary>
    /// Dado o mesmo CPF com e sem máscara, quando o hash é calculado, então é igual, tem 32 bytes e muda com a chave.
    /// </summary>
    [Fact]
    public void CalcularHash_MesmoCpf_EhDeterministicoEDependeDaChave()
    {
        var hash = _cifrador.CalcularHash(_cpf);

        hash.Length.ShouldBe(32);
        hash.ShouldBe(_cifrador.CalcularHash(Cpf.Criar("52998224725").Valor));
        hash.ShouldNotBe(new CifradorCpfAesGcm(Opcoes()).CalcularHash(_cpf));
    }

    /// <summary>
    /// Dadas chaves ausentes, fora do Base64 ou de tamanho errado, quando o cifrador é criado, então a configuração é recusada sem revelar a chave.
    /// </summary>
    /// <param name="cifra">Chave de cifra.</param>
    /// <param name="hash">Chave de hash.</param>
    [Theory]
    [InlineData("", "")]
    [InlineData("nao-e-base64!!", "")]
    [InlineData("AAAA", "AAAA")]
    public void Construtor_ChavesInvalidas_LancaExcecao(string cifra, string hash)
    {
        var opcoes = new CpfOpcoes { ChaveCifra = cifra, ChaveHash = hash };

        var excecao = Should.Throw<ArgumentException>(() => new CifradorCpfAesGcm(opcoes));
        excecao.Message.ShouldNotContain(cifra.Length == 0 ? "\0" : cifra);
    }

    /// <summary>
    /// Dada uma chave nula na configuração, quando o cifrador é criado, então a configuração é recusada.
    /// </summary>
    [Fact]
    public void Construtor_ChaveNula_LancaExcecao()
    {
        var opcoes = new CpfOpcoes { ChaveCifra = null!, ChaveHash = null! };

        Should.Throw<ArgumentException>(() => new CifradorCpfAesGcm(opcoes));
    }

    /// <summary>
    /// Dado uma chave de hash inválida com cifra válida, quando o cifrador é criado, então recusa.
    /// </summary>
    [Fact]
    public void Construtor_ChaveDeHashInvalida_LancaExcecao()
    {
        var opcoes = new CpfOpcoes { ChaveCifra = Chave(), ChaveHash = "AAAA" };

        Should.Throw<ArgumentException>(() => new CifradorCpfAesGcm(opcoes));
    }

    /// <summary>
    /// Dados argumentos nulos, quando usados, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => new CifradorCpfAesGcm(null!));
        Should.Throw<ArgumentNullException>(() => _cifrador.Cifrar(null!));
        Should.Throw<ArgumentNullException>(() => _cifrador.Decifrar(null!));
        Should.Throw<ArgumentNullException>(() => _cifrador.CalcularHash(null!));
    }

    /// <summary>
    /// Cria opções com chaves aleatórias de 32 bytes.
    /// </summary>
    /// <returns>As opções.</returns>
    private static CpfOpcoes Opcoes()
    {
        return new CpfOpcoes { ChaveCifra = Chave(), ChaveHash = Chave(), VersaoChave = 1 };
    }

    /// <summary>
    /// Gera uma chave aleatória de 32 bytes em Base64.
    /// </summary>
    /// <returns>A chave.</returns>
    private static string Chave()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
