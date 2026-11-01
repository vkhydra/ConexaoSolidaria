using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes da página de cadastro: formulário, link da política, validação, mensagens de erro e abertura do login.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CadastroTests : BunitContext
{
    /// <summary>
    /// Cliente da Identity falso.
    /// </summary>
    private readonly IClienteIdentity _identity = Substitute.For<IClienteIdentity>();

    /// <summary>
    /// Registra o falso no contêiner do bUnit.
    /// </summary>
    public CadastroTests()
    {
        Services.AddSingleton(_identity);
        AddAuthorization();
    }

    /// <summary>
    /// Dada a página de cadastro, quando exibida, então tem os campos, o aceite e o link para a política de privacidade.
    /// </summary>
    [Fact]
    public void Pagina_Exibida_TemCamposEAceiteComLinkDaPolitica()
    {
        var pagina = Render<Cadastro>();

        pagina.Find("h1").TextContent.ShouldBe("Criar conta de doador");
        pagina.FindAll("input[type='text'], input:not([type])").Count.ShouldBe(2);
        pagina.Find("input[type='email']").ShouldNotBeNull();
        pagina.Find("input[type='password']").ShouldNotBeNull();
        pagina.Find("input[type='checkbox']").ShouldNotBeNull();
        pagina.Find("a[href='/politica-de-privacidade']").TextContent.ShouldBe("política de privacidade");
        pagina.Find("button[type='submit']").TextContent.ShouldBe("Cadastrar");
    }

    /// <summary>
    /// Dado um cadastro válido, quando enviado, então a API recebe os dados aparados e o usuário vai ao login com o aviso.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Enviar_CadastroValido_VaiAoLoginComAviso()
    {
        _identity.CadastrarAsync(Arg.Any<CadastroDoadorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Ok(new DoadorResposta(Guid.NewGuid(), "Maria Silva", "m@x.com", "***.982.247-**", DateTimeOffset.UtcNow)));
        var pagina = Render<Cadastro>();
        Preencher(pagina.Instance.Modelo!);

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);

        await _identity.Received(1).CadastrarAsync(new CadastroDoadorRequest("Maria Silva", "m@x.com", "529.982.247-25", "Senha@123", true), Arg.Any<CancellationToken>());
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/entrar?cadastrado=true");
        pagina.Instance.Mensagens.ShouldBeEmpty();
    }

    /// <summary>
    /// Dado um e-mail ou CPF já cadastrado, quando enviado, então a mensagem diz qual está em uso.
    /// </summary>
    /// <param name="codigo">Código do erro da API.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData("doador.email_em_uso", "Este e-mail já está em uso.")]
    [InlineData("doador.cpf_em_uso", "Este CPF já está cadastrado.")]
    public async Task Enviar_EmailOuCpfEmUso_MostraAMensagem(string codigo, string mensagem)
    {
        _identity.CadastrarAsync(Arg.Any<CadastroDoadorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<DoadorResposta>(new ErroApi(409, codigo, "Conflito", new Dictionary<string, string[]>(), null)));
        var pagina = Render<Cadastro>();
        Preencher(pagina.Instance.Modelo!);

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);
        pagina.Render();

        pagina.Find("[data-teste='mensagens'] li").TextContent.ShouldBe(mensagem);
    }

    /// <summary>
    /// Dados erros de validação por campo, quando enviado, então as mensagens da API aparecem sem repetição.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Enviar_Erro400ComCampos_MostraMensagensDaApi()
    {
        var campos = new Dictionary<string, string[]> { ["cpf"] = ["CPF inválido."], ["senha"] = ["A senha não atende aos requisitos.", "CPF inválido."] };
        _identity.CadastrarAsync(Arg.Any<CadastroDoadorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<DoadorResposta>(new ErroApi(400, "validacao", "Dados inválidos", campos, null)));
        var pagina = Render<Cadastro>();
        Preencher(pagina.Instance.Modelo!);

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);

        pagina.Instance.Mensagens.ShouldBe(["CPF inválido.", "A senha não atende aos requisitos."]);
    }

    /// <summary>
    /// Dados outros erros, quando enviado, então a mensagem é a do limite de tentativas ou a da API com o código de rastreio.
    /// </summary>
    /// <param name="status">Status da API.</param>
    /// <param name="rastreio">Código de rastreio, se houver.</param>
    /// <param name="mensagem">Mensagem esperada.</param>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Theory]
    [InlineData(429, null, "Muitas tentativas. Tente novamente em alguns minutos.")]
    [InlineData(503, "t9", "Fora do ar. Código de rastreio: t9.")]
    [InlineData(500, null, "Fora do ar.")]
    public async Task Enviar_OutrosErros_MostraMensagemAdequada(int status, string? rastreio, string mensagem)
    {
        _identity.CadastrarAsync(Arg.Any<CadastroDoadorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<DoadorResposta>(new ErroApi(status, null, "Fora do ar.", new Dictionary<string, string[]>(), rastreio)));
        var pagina = Render<Cadastro>();
        Preencher(pagina.Instance.Modelo!);

        await pagina.InvokeAsync(pagina.Instance.EnviarAsync);

        pagina.Instance.Mensagens.ShouldBe([mensagem]);
    }

    /// <summary>
    /// Dado o login aberto depois do cadastro, quando a página de login é exibida com o parâmetro, então mostra o aviso de conta criada.
    /// </summary>
    [Fact]
    public void Entrar_DepoisDoCadastro_MostraAviso()
    {
        Services.AddSingleton(Substitute.For<IServicoDeSessao>());
        var navegacao = Services.GetRequiredService<NavigationManager>();
        navegacao.NavigateTo("/entrar?cadastrado=true");

        var pagina = Render<Entrar>();

        pagina.Find("[data-teste='cadastrado']").TextContent.ShouldBe("Conta criada. Entre para continuar.");
    }

    /// <summary>
    /// Dada a página da política de privacidade, quando exibida, então mostra a versão e o direito de excluir os dados.
    /// </summary>
    [Fact]
    public void Politica_Exibida_MostraVersaoEDireitos()
    {
        var pagina = Render<PoliticaDePrivacidade>();

        pagina.Find("h1").TextContent.ShouldBe("Política de privacidade");
        pagina.Markup.ShouldContain("Versão 1.0");
        pagina.Markup.ShouldContain("Meus dados");
        pagina.Markup.ShouldContain("dpo@esperancasolidaria.example");
    }

    /// <summary>
    /// Preenche o modelo com um cadastro válido, com espaços nas pontas para provar que são aparados.
    /// </summary>
    /// <param name="modelo">Modelo do formulário.</param>
    private static void Preencher(Cadastro.ModeloDeCadastro modelo)
    {
        modelo.NomeCompleto = "  Maria Silva ";
        modelo.Email = " m@x.com ";
        modelo.Cpf = " 529.982.247-25 ";
        modelo.Senha = "Senha@123";
        modelo.AceitePolitica = true;
    }
}
