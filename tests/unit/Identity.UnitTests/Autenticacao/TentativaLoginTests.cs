using Identity.Domain.Autenticacao;

namespace Identity.UnitTests.Autenticacao;

/// <summary>
/// Testes do controle de tentativas de login: contagem, janela de 15 minutos e bloqueio na quinta falha.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TentativaLoginTests
{
    /// <summary>
    /// Instante fixo que abre o cenário dos testes.
    /// </summary>
    private static readonly DateTimeOffset _inicio = new(2026, 11, 10, 14, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Hash de e-mail fictício.
    /// </summary>
    private static readonly byte[] _hash = [1, 2, 3, 4];

    /// <summary>
    /// Dado um controle novo, quando criado, então não tem falhas nem bloqueio.
    /// </summary>
    [Fact]
    public void Nova_ControleNovo_ComecaSemFalhasNemBloqueio()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);

        tentativa.EmailHash.ShouldBe(_hash);
        tentativa.Falhas.ShouldBe(0);
        tentativa.JanelaInicio.ShouldBe(_inicio);
        tentativa.BloqueadoAte.ShouldBeNull();
        tentativa.EstaBloqueado(_inicio).ShouldBeFalse();
    }

    /// <summary>
    /// Dado um hash nulo, quando o controle é criado, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Nova_HashNulo_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => TentativaLogin.Nova(null!, _inicio));
    }

    /// <summary>
    /// Dadas quatro falhas, quando conferido, então o e-mail ainda não está bloqueado.
    /// </summary>
    [Fact]
    public void RegistrarFalha_QuatroFalhas_NaoBloqueia()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);

        for (var i = 0; i < TentativaLogin.LimiteDeFalhas - 1; i++)
        {
            tentativa.RegistrarFalha(_inicio.AddMinutes(i));
        }

        tentativa.Falhas.ShouldBe(4);
        tentativa.EstaBloqueado(_inicio.AddMinutes(4)).ShouldBeFalse();
    }

    /// <summary>
    /// Dadas cinco falhas dentro da janela, quando a quinta acontece, então o e-mail fica bloqueado por 15 minutos.
    /// </summary>
    [Fact]
    public void RegistrarFalha_QuintaFalhaNaJanela_BloqueiaPor15Minutos()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);
        var quinta = _inicio.AddMinutes(10);

        for (var i = 0; i < TentativaLogin.LimiteDeFalhas - 1; i++)
        {
            tentativa.RegistrarFalha(_inicio.AddMinutes(i));
        }

        tentativa.RegistrarFalha(quinta);

        tentativa.Falhas.ShouldBe(5);
        tentativa.BloqueadoAte.ShouldBe(quinta + TentativaLogin.Bloqueio);
        tentativa.EstaBloqueado(quinta).ShouldBeTrue();
        tentativa.EstaBloqueado(quinta.AddMinutes(14)).ShouldBeTrue();
        tentativa.EstaBloqueado(quinta.AddMinutes(15)).ShouldBeFalse();
    }

    /// <summary>
    /// Dadas falhas espalhadas por mais de 15 minutos, quando a janela expira, então a contagem recomeça.
    /// </summary>
    [Fact]
    public void RegistrarFalha_AposJanelaExpirada_ReiniciaContagem()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);
        tentativa.RegistrarFalha(_inicio);
        tentativa.RegistrarFalha(_inicio.AddMinutes(1));
        tentativa.RegistrarFalha(_inicio.AddMinutes(2));

        var depois = _inicio.AddMinutes(16);
        tentativa.RegistrarFalha(depois);

        tentativa.Falhas.ShouldBe(1);
        tentativa.JanelaInicio.ShouldBe(depois);
        tentativa.BloqueadoAte.ShouldBeNull();
    }

    /// <summary>
    /// Dado um bloqueio que terminou, quando nova falha acontece, então a contagem recomeça em uma falha.
    /// </summary>
    [Fact]
    public void RegistrarFalha_AposFimDoBloqueio_ReiniciaContagem()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);
        for (var i = 0; i < TentativaLogin.LimiteDeFalhas; i++)
        {
            tentativa.RegistrarFalha(_inicio);
        }

        var depois = _inicio + TentativaLogin.Bloqueio + TimeSpan.FromSeconds(1);
        tentativa.RegistrarFalha(depois);

        tentativa.EstaBloqueado(depois).ShouldBeFalse();
        tentativa.Falhas.ShouldBe(1);
        tentativa.BloqueadoAte.ShouldBeNull();
    }

    /// <summary>
    /// Dadas falhas acumuladas, quando o login tem sucesso, então a contagem e o bloqueio são zerados.
    /// </summary>
    [Fact]
    public void RegistrarSucesso_AposFalhas_ZeraContagemEBloqueio()
    {
        var tentativa = TentativaLogin.Nova(_hash, _inicio);
        for (var i = 0; i < TentativaLogin.LimiteDeFalhas; i++)
        {
            tentativa.RegistrarFalha(_inicio);
        }

        var depois = _inicio.AddHours(1);
        tentativa.RegistrarSucesso(depois);

        tentativa.Falhas.ShouldBe(0);
        tentativa.BloqueadoAte.ShouldBeNull();
        tentativa.JanelaInicio.ShouldBe(depois);
        tentativa.EstaBloqueado(depois).ShouldBeFalse();
    }
}
