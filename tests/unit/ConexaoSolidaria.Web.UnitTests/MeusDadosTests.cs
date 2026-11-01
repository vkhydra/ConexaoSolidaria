using System.Reflection;
using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da página Meus dados (direitos do titular): exibição com CPF mascarado, confirmação antes de excluir e saída da sessão.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MeusDadosTests : BunitContext
{
    /// <summary>
    /// Dados do titular usados nos cenários.
    /// </summary>
    private static readonly MeusDadosResposta _dados = new(Guid.NewGuid(), "Maria Silva", "maria@exemplo.com", "***.982.247-**", new DateTimeOffset(2026, 11, 10, 14, 3, 22, TimeSpan.FromHours(-3)), new ConsentimentoResposta("2026-10", new DateTimeOffset(2026, 11, 10, 14, 3, 22, TimeSpan.FromHours(-3))));

    /// <summary>
    /// Cliente da Identity falso.
    /// </summary>
    private readonly IClienteIdentity _identity = Substitute.For<IClienteIdentity>();

    /// <summary>
    /// Registra o falso no contêiner do bUnit.
    /// </summary>
    public MeusDadosTests()
    {
        Services.AddSingleton(_identity);
        AddAuthorization().SetAuthorized("Maria");
    }

    /// <summary>
    /// Dada a página, quando inspecionada, então exige o papel Doador.
    /// </summary>
    [Fact]
    public void Pagina_ExigeOPapelDoador()
    {
        typeof(MeusDados).GetCustomAttribute<AuthorizeAttribute>()!.Roles.ShouldBe("Doador");
    }

    /// <summary>
    /// Dado um titular, quando a página abre, então mostra nome, e-mail, CPF mascarado, data de cadastro e versão da política.
    /// </summary>
    [Fact]
    public void Pagina_MostraOsDadosComCpfMascarado()
    {
        _identity.ObterMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(_dados));

        var pagina = Render<MeusDados>();

        var texto = pagina.Find("[data-teste='dados']").TextContent;
        texto.ShouldContain("Maria Silva");
        texto.ShouldContain("maria@exemplo.com");
        texto.ShouldContain("***.982.247-**");
        texto.ShouldContain("10/11/2026 14:03");
        texto.ShouldContain("versão 2026-10");
        pagina.Markup.ShouldNotContain("529.982.247-25");
    }

    /// <summary>
    /// Dadas falhas ao carregar, quando a página abre, então mostra a mensagem de sessão expirada (401) ou a da API.
    /// </summary>
    /// <param name="status">Status da API.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    [Theory]
    [InlineData(401, "Sua sessão expirou. Entre de novo.")]
    [InlineData(403, "Você não tem permissão para esta ação.")]
    public void Pagina_FalhaAoCarregar_MostraMensagem(int status, string mensagem)
    {
        _identity.ObterMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha<MeusDadosResposta>(new ErroApi(status, null, "x", new Dictionary<string, string[]>(), null)));

        var pagina = Render<MeusDados>();

        pagina.Find("[data-teste='mensagem']").TextContent.ShouldBe(mensagem);
        pagina.FindAll("[data-teste='dados'], [data-teste='excluir']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dado o botão de excluir, quando clicado, então abre a confirmação sem chamar a API; voltar a fecha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Excluir_PedeConfirmacaoAntes()
    {
        _identity.ObterMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(_dados));
        var pagina = Render<MeusDados>();

        pagina.Find("[data-teste='excluir']").Click();
        pagina.Find("[data-teste='confirmacao']").TextContent.ShouldContain("Isso não pode ser desfeito.");
        pagina.Find("[data-teste='desistir']").Click();

        pagina.FindAll("[data-teste='confirmacao']").ShouldBeEmpty();
        await _identity.DidNotReceive().ExcluirMeusDadosAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada a confirmação, quando a API responde 204, então a sessão é encerrada pela saída do servidor.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Excluir_Confirmado_EncerraASessao()
    {
        _identity.ObterMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(_dados));
        _identity.ExcluirMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok());
        var pagina = Render<MeusDados>();
        pagina.Find("[data-teste='excluir']").Click();

        pagina.Find("[data-teste='confirmar']").Click();

        await _identity.Received(1).ExcluirMeusDadosAsync(Arg.Any<CancellationToken>());
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith(MeusDados.EnderecoDeSaida);
        pagina.Instance.Excluindo.ShouldBeFalse();
    }

    /// <summary>
    /// Dada uma falha na exclusão, quando confirmada, então a mensagem aparece, a confirmação fecha e a sessão continua.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Excluir_Falha_MostraMensagemEMantemASessao()
    {
        _identity.ObterMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(_dados));
        _identity.ExcluirMeusDadosAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha(new ErroApi(503, "servico.indisponivel", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<MeusDados>();
        pagina.Find("[data-teste='excluir']").Click();

        pagina.Find("[data-teste='confirmar']").Click();

        await _identity.Received(1).ExcluirMeusDadosAsync(Arg.Any<CancellationToken>());
        pagina.Find("[data-teste='mensagem']").TextContent.ShouldContain("indisponível");
        pagina.FindAll("[data-teste='confirmacao']").ShouldBeEmpty();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldNotEndWith(MeusDados.EnderecoDeSaida);
    }

    /// <summary>
    /// Dado o endpoint de fim de sessão após a exclusão, quando chamado, então apaga o cookie e volta ao início com o aviso; a página inicial o mostra.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task SessaoEncerrada_ApagaOCookieEVoltaComAviso()
    {
        var sessao = Substitute.For<IServicoDeSessao>();
        var contexto = new DefaultHttpContext();

        var resultado = await EndpointsDeSessao.EncerrarAposExclusaoAsync(contexto, sessao);

        resultado.ShouldBeOfType<RedirectHttpResult>().Url.ShouldBe("/?excluida=true");
        await sessao.Received(1).SairAsync(contexto);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/?excluida=true");
        Render<Home>().Find("[data-teste='excluida']").TextContent.ShouldBe("Seus dados foram excluídos e a sessão foi encerrada.");
    }
}
