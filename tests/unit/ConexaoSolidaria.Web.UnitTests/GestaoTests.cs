using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Bunit;
using ConexaoSolidaria.Web.Components.Pages.Gestao;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes das páginas de gestão de campanhas: lista, criação, edição com versão, confirmação de status e mensagens de erro.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GestaoTests : BunitContext
{
    /// <summary>
    /// Identificador da campanha usada nos cenários.
    /// </summary>
    private static readonly Guid _id = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");

    /// <summary>
    /// Cliente de Campanhas falso.
    /// </summary>
    private readonly IClienteCampanhas _campanhas = Substitute.For<IClienteCampanhas>();

    /// <summary>
    /// Registra o falso no contêiner do bUnit.
    /// </summary>
    public GestaoTests()
    {
        Services.AddSingleton(_campanhas);
        AddAuthorization().SetAuthorized("Gestora");
    }

    /// <summary>
    /// Dadas as páginas de gestão, quando inspecionadas, então todas exigem o papel GestorONG.
    /// </summary>
    /// <param name="tipo">Tipo da página.</param>
    [Theory]
    [InlineData(typeof(ListaDeCampanhas))]
    [InlineData(typeof(NovaCampanha))]
    [InlineData(typeof(EditarCampanha))]
    public void Paginas_ExigemOPapelGestorOng(Type tipo)
    {
        tipo.GetCustomAttribute<AuthorizeAttribute>()!.Roles.ShouldBe("GestorONG");
    }

    /// <summary>
    /// Dada uma lista de campanhas, quando a página é aberta, então mostra a tabela com valores em reais e o link para abrir.
    /// </summary>
    [Fact]
    public void Lista_ComCampanhas_MostraTabela()
    {
        _campanhas.ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Pagina(1, 1, Campanha("Ativa"))));

        var pagina = Render<ListaDeCampanhas>();

        var linha = pagina.Find("[data-teste='linha']");
        linha.TextContent.ShouldContain("Natal");
        linha.TextContent.ShouldContain("R$ 10.000,00");
        linha.QuerySelector("a")!.GetAttribute("href").ShouldBe($"/gestao/campanhas/{_id}");
        pagina.Find("a[href='/gestao/campanhas/nova']").ShouldNotBeNull();
    }

    /// <summary>
    /// Dado o filtro de status, quando alterado, então a lista é recarregada pelo status; voltar a Todos remove o filtro.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Lista_MudarFiltro_RecarregaPeloStatus()
    {
        _campanhas.ListarAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Pagina(1, 1, Campanha("Ativa"))));
        var pagina = Render<ListaDeCampanhas>();

        pagina.Find("[data-teste='filtro']").Change("Cancelada");
        pagina.Find("[data-teste='filtro']").Change(string.Empty);

        await _campanhas.Received(1).ListarAsync("Cancelada", 1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _campanhas.Received(2).ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        pagina.Instance.Filtro.ShouldBeNull();
    }

    /// <summary>
    /// Dado um resultado com várias páginas, quando se clica em Próxima e em Anterior, então a lista pede a página certa.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Lista_Paginacao_PedeAPaginaCerta()
    {
        _campanhas.ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Pagina(1, 2, Campanha("Ativa"))));
        _campanhas.ListarAsync(null, 2, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Pagina(2, 2, Campanha("Concluida"))));
        var pagina = Render<ListaDeCampanhas>();
        pagina.FindAll("[data-teste='anterior']").ShouldBeEmpty();

        pagina.Find("[data-teste='proxima']").Click();
        pagina.Find("[data-teste='anterior']").Click();

        await _campanhas.Received(2).ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _campanhas.Received(1).ListarAsync(null, 2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma lista vazia ou uma falha, quando a página é aberta, então mostra a mensagem correspondente.
    /// </summary>
    [Fact]
    public void Lista_VaziaOuComFalha_MostraMensagem()
    {
        _campanhas.ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new PaginaDeCampanhas([], 1, 20, 0, 0)));
        Render<ListaDeCampanhas>().Find("[data-teste='vazio']").TextContent.ShouldBe("Nenhuma campanha encontrada.");

        _campanhas.ListarAsync(null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha<PaginaDeCampanhas>(Erro(403)));
        Render<ListaDeCampanhas>().Find("[data-teste='mensagem']").TextContent.ShouldBe("Você não tem permissão para esta ação.");
    }

    /// <summary>
    /// Dado um formulário válido, quando a campanha é criada, então a API recebe os dados aparados e a tela abre a campanha.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Nova_Valida_CriaEAbreACampanha()
    {
        _campanhas.CriarAsync(Arg.Any<CampanhaRequest>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Campanha("Ativa")));
        var pagina = Render<NovaCampanha>();
        PreencherModelo(pagina.Instance.Modelo);

        await pagina.InvokeAsync(pagina.Instance.SalvarAsync);

        await _campanhas.Received(1).CriarAsync(new CampanhaRequest("Natal", "Descrição", new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 24), 5000m), Arg.Any<CancellationToken>());
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith($"/gestao/campanhas/{_id}");
        pagina.Instance.Enviando.ShouldBeFalse();
    }

    /// <summary>
    /// Dada uma regra de negócio violada, quando a campanha é criada, então a mensagem do manual aparece e o formulário continua.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Nova_RegraViolada_MostraMensagemDoManual()
    {
        _campanhas.CriarAsync(Arg.Any<CampanhaRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<CampanhaGestao>(new ErroApi(422, "campanha.data_fim_passada", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<NovaCampanha>();
        PreencherModelo(pagina.Instance.Modelo);

        await pagina.InvokeAsync(pagina.Instance.SalvarAsync);
        pagina.Render();

        pagina.Find("[data-teste='mensagens'] li").TextContent.ShouldBe("A data de término não pode estar no passado.");
        pagina.Find("input").ShouldNotBeNull();
    }

    /// <summary>
    /// Dada uma campanha ativa, quando a página de edição é aberta, então mostra o resumo, o formulário e as ações de status.
    /// </summary>
    [Fact]
    public void Editar_CampanhaAtiva_MostraFormularioEAcoes()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")));

        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));

        pagina.Find("[data-teste='resumo']").TextContent.ShouldContain("Ativa");
        pagina.Find("input").GetAttribute("value").ShouldBe("Natal");
        pagina.Find("[data-teste='concluir']").ShouldNotBeNull();
        pagina.Find("[data-teste='cancelar']").ShouldNotBeNull();
        pagina.FindAll("[data-teste='nao-editavel']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma campanha concluída ou cancelada, quando aberta, então não há formulário nem ações, só o aviso de não editável.
    /// </summary>
    /// <param name="status">Status da campanha.</param>
    [Theory]
    [InlineData("Concluida")]
    [InlineData("Cancelada")]
    public void Editar_CampanhaEncerrada_MostraSoOAviso(string status)
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha(status), "\"3\"")));

        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));

        pagina.Find("[data-teste='nao-editavel']").TextContent.ShouldContain("não pode ser editada");
        pagina.FindAll("form, [data-teste='concluir'], [data-teste='cancelar']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma campanha inexistente, quando a página é aberta, então mostra a mensagem de não encontrada.
    /// </summary>
    [Fact]
    public void Editar_Inexistente_MostraNaoEncontrada()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha<CampanhaComVersao>(new ErroApi(404, "campanha.nao_encontrada", "x", new Dictionary<string, string[]>(), null)));

        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));

        pagina.Find("[data-teste='carga']").TextContent.ShouldBe("Campanha não encontrada.");
    }

    /// <summary>
    /// Dada uma edição, quando salva, então a versão lida vai em If-Match e a tela recarrega com o aviso de sucesso.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Editar_Salvar_EnviaAVersaoLida()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")), ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"4\"")));
        _campanhas.AtualizarAsync(_id, "\"3\"", Arg.Any<CampanhaRequest>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(Campanha("Ativa")));
        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));
        pagina.Instance.Modelo.Titulo = "Natal Novo";

        await pagina.InvokeAsync(pagina.Instance.SalvarAsync);
        pagina.Render();

        await _campanhas.Received(1).AtualizarAsync(_id, "\"3\"", Arg.Is<CampanhaRequest>(r => r.Titulo == "Natal Novo"), Arg.Any<CancellationToken>());
        pagina.Find("[data-teste='aviso']").TextContent.ShouldBe("Alterações salvas.");
    }

    /// <summary>
    /// Dada uma edição simultânea, quando a API responde 409, então avisa para recarregar e mantém o formulário.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Editar_ConflitoDeVersao_AvisaParaRecarregar()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")));
        _campanhas.AtualizarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CampanhaRequest>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<CampanhaGestao>(new ErroApi(409, "campanha.conflito_de_versao", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));

        await pagina.InvokeAsync(pagina.Instance.SalvarAsync);
        pagina.Render();

        pagina.Find("[data-teste='mensagens'] li").TextContent.ShouldStartWith("A campanha foi alterada por outra pessoa.");
    }

    /// <summary>
    /// Dado o pedido de concluir ou cancelar, quando o gestor confirma, então o status muda e a tela recarrega com o aviso.
    /// </summary>
    /// <param name="botao">Botão clicado.</param>
    /// <param name="status">Status de destino.</param>
    /// <param name="aviso">Aviso esperado.</param>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Theory]
    [InlineData("concluir", "Concluida", "Campanha concluída.")]
    [InlineData("cancelar", "Cancelada", "Campanha cancelada.")]
    public async Task Editar_ConfirmarStatus_AlteraERecarrega(string botao, string status, string aviso)
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")), ResultadoApi.Ok(new CampanhaComVersao(Campanha(status), "\"4\"")));
        _campanhas.AlterarStatusAsync(_id, status, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok());
        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));

        pagina.Find($"[data-teste='{botao}']").Click();
        pagina.Find("[data-teste='confirmacao']").TextContent.ShouldContain("não pode ser desfeita");
        pagina.Find("[data-teste='confirmar']").Click();

        await _campanhas.Received(1).AlterarStatusAsync(_id, status, Arg.Any<CancellationToken>());
        pagina.Find("[data-teste='aviso']").TextContent.ShouldBe(aviso);
        pagina.FindAll("[data-teste='confirmacao']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada a confirmação aberta, quando o gestor desiste, então nada é alterado; confirmar sem pedido aberto não faz nada.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Editar_Desistir_NaoAlteraNada()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")));
        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));
        await pagina.InvokeAsync(pagina.Instance.AlterarStatusAsync);

        pagina.Find("[data-teste='cancelar']").Click();
        pagina.Find("[data-teste='desistir']").Click();

        await _campanhas.DidNotReceiveWithAnyArgs().AlterarStatusAsync(default, default!, Xunit.TestContext.Current.CancellationToken);
        pagina.FindAll("[data-teste='confirmacao']").ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma transição recusada, quando confirmada, então a mensagem de transição inválida aparece.
    /// </summary>
    /// <returns>Tarefa que termina com a verificação feita.</returns>
    [Fact]
    public async Task Editar_TransicaoRecusada_MostraMensagem()
    {
        _campanhas.ObterAsync(_id, Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new CampanhaComVersao(Campanha("Ativa"), "\"3\"")));
        _campanhas.AlterarStatusAsync(_id, "Cancelada", Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha(new ErroApi(422, "campanha.transicao_invalida", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<EditarCampanha>(p => p.Add(c => c.Id, _id));
        pagina.Find("[data-teste='cancelar']").Click();

        pagina.Find("[data-teste='confirmar']").Click();

        pagina.Find("[data-teste='mensagens'] li").TextContent.ShouldBe("A mudança de status não é permitida para esta campanha.");
    }

    /// <summary>
    /// Dado o modelo do formulário, quando validado, então aplica tamanhos, meta e a ordem das datas do manual.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="meta">Meta.</param>
    /// <param name="inicio">Dia de início em novembro de 2026.</param>
    /// <param name="fim">Dia de término em novembro de 2026.</param>
    /// <param name="validos">Se o modelo deve ser válido.</param>
    [Theory]
    [InlineData("Natal", "5000", 10, 24, true)]
    [InlineData("ab", "5000", 10, 24, false)]
    [InlineData("Natal", "0", 10, 24, false)]
    [InlineData("Natal", "100000000", 10, 24, false)]
    [InlineData("Natal", "5000", 24, 10, false)]
    [InlineData("Natal", "5000", 10, 10, true)]
    public void Modelo_Validacao_SegueOManual(string titulo, string meta, int inicio, int fim, bool validos)
    {
        var modelo = new ModeloDeCampanha { Titulo = titulo, MetaFinanceira = decimal.Parse(meta, System.Globalization.CultureInfo.InvariantCulture), DataInicio = new DateOnly(2026, 11, inicio), DataFim = new DateOnly(2026, 11, fim) };
        var resultados = new List<ValidationResult>();

        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true).ShouldBe(validos);
    }

    /// <summary>
    /// Dados códigos e campos, quando traduzidos, então saem as mensagens do manual, sem repetição.
    /// </summary>
    [Fact]
    public void TradutorDeErros_Casos_SaiMensagemCerta()
    {
        TradutorDeErros.Mensagens(new ErroApi(400, "validacao", "t", new Dictionary<string, string[]> { ["a"] = ["x", "y"], ["b"] = ["x"] }, null)).ShouldBe(["x", "y"]);
        TradutorDeErros.Mensagens(Erro(403)).ShouldBe(["Você não tem permissão para esta ação."]);
        TradutorDeErros.Mensagens(new ErroApi(500, "desconhecido", "Falha.", new Dictionary<string, string[]>(), "t7")).ShouldBe(["Falha. Código de rastreio: t7."]);
        TradutorDeErros.Mensagens(new ErroApi(500, null, "Falha.", new Dictionary<string, string[]>(), null)).ShouldBe(["Falha."]);
        Should.Throw<ArgumentNullException>(() => TradutorDeErros.Mensagens(null!));
    }

    /// <summary>
    /// Cria uma campanha de gestão.
    /// </summary>
    /// <param name="status">Status da campanha.</param>
    /// <returns>A campanha.</returns>
    private static CampanhaGestao Campanha(string status)
    {
        return new CampanhaGestao(_id, "Natal", "Descrição", new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 24), 10000m, 150m, 1.5m, status);
    }

    /// <summary>
    /// Cria uma página de campanhas.
    /// </summary>
    /// <param name="numero">Número da página.</param>
    /// <param name="total">Total de páginas.</param>
    /// <param name="campanha">Campanha da página.</param>
    /// <returns>A página.</returns>
    private static PaginaDeCampanhas Pagina(int numero, int total, CampanhaGestao campanha)
    {
        return new PaginaDeCampanhas([campanha], numero, 20, total * 20, total);
    }

    /// <summary>
    /// Cria um erro de API sem detalhes.
    /// </summary>
    /// <param name="status">Status HTTP.</param>
    /// <returns>O erro.</returns>
    private static ErroApi Erro(int status)
    {
        return new ErroApi(status, null, "Erro.", new Dictionary<string, string[]>(), null);
    }

    /// <summary>
    /// Preenche o modelo com uma campanha válida, com espaços nas pontas para provar que são aparados.
    /// </summary>
    /// <param name="modelo">Modelo do formulário.</param>
    private static void PreencherModelo(ModeloDeCampanha modelo)
    {
        modelo.Titulo = " Natal ";
        modelo.Descricao = " Descrição ";
        modelo.DataInicio = new DateOnly(2026, 11, 10);
        modelo.DataFim = new DateOnly(2026, 12, 24);
        modelo.MetaFinanceira = 5000m;
    }
}
