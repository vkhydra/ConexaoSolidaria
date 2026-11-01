using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Bunit;
using ConexaoSolidaria.Web.Components.Pages;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ConexaoSolidaria.Web.UnitTests;

/// <summary>
/// Testes do fluxo de doação: papel exigido, botão desabilitado no envio, chave de idempotência por tentativa e mensagens.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DoarTests : BunitContext
{
    /// <summary>
    /// Campanha usada nos cenários.
    /// </summary>
    private static readonly Guid _campanha = Guid.Parse("0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f");

    /// <summary>
    /// Cliente de Campanhas falso.
    /// </summary>
    private readonly IClienteCampanhas _campanhas = Substitute.For<IClienteCampanhas>();

    /// <summary>
    /// Registra o falso e deixa o painel sem a campanha por padrão.
    /// </summary>
    public DoarTests()
    {
        Services.AddSingleton(_campanhas);
        AddAuthorization().SetAuthorized("Maria");
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok<IReadOnlyList<PainelItem>>([]));
    }

    /// <summary>
    /// Dada a página de doação, quando inspecionada, então exige o papel Doador.
    /// </summary>
    [Fact]
    public void Pagina_ExigeOPapelDoador()
    {
        typeof(Doar).GetCustomAttribute<AuthorizeAttribute>()!.Roles.ShouldBe("Doador");
    }

    /// <summary>
    /// Dada uma campanha no painel, quando a página abre, então o título aparece; sem ela, o título fica genérico.
    /// </summary>
    [Fact]
    public void Pagina_TituloVemDoPainel()
    {
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok<IReadOnlyList<PainelItem>>([new PainelItem(_campanha, "Natal Solidário", 100m, 0m, 0m, new DateOnly(2026, 12, 24))]));

        Render<Doar>(p => p.Add(c => c.Id, _campanha)).Find("h1").TextContent.ShouldBe("Doar para Natal Solidário");
        _campanhas.ObterPainelAsync(Arg.Any<CancellationToken>()).Returns(ResultadoApi.Falha<IReadOnlyList<PainelItem>>(new ErroApi(503, null, "x", new Dictionary<string, string[]>(), null)));
        Render<Doar>(p => p.Add(c => c.Id, _campanha)).Find("h1").TextContent.ShouldBe("Doar");
    }

    /// <summary>
    /// Dada uma doação aceita, quando enviada, então a tela mostra a mensagem de processamento e o número da doação.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Doar_Aceita_MostraMensagemENumero()
    {
        var doacao = Guid.Parse("0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f");
        _campanhas.DoarAsync(_campanha, 150.50m, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ResultadoApi.Ok(new DoacaoAceita(doacao, "Recebida", "ok")));
        var pagina = Render<Doar>(p => p.Add(c => c.Id, _campanha));
        pagina.Instance.Modelo.Valor = 150.505m;

        await pagina.InvokeAsync(pagina.Instance.DoarAsync);
        pagina.Render();

        pagina.Find("[data-teste='sucesso']").TextContent.ShouldContain("Doação recebida. O total será atualizado em instantes.");
        pagina.Find("[data-teste='numero']").TextContent.ShouldBe(doacao.ToString());
        pagina.FindAll("form").ShouldBeEmpty();
        pagina.Instance.Enviando.ShouldBeFalse();
    }

    /// <summary>
    /// Dado o mesmo valor reenviado após uma falha, quando a tentativa se repete, então a chave de idempotência é a mesma; mudar o valor gera outra.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Doar_Reenvio_MantemAChaveEMudarOValorTrocaAChave()
    {
        _campanhas.DoarAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<DoacaoAceita>(new ErroApi(503, "servico.indisponivel", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<Doar>(p => p.Add(c => c.Id, _campanha));
        pagina.Instance.Modelo.Valor = 50m;
        var chaveInicial = pagina.Instance.ChaveDaTentativa;

        await pagina.InvokeAsync(pagina.Instance.DoarAsync);
        await pagina.InvokeAsync(pagina.Instance.DoarAsync);
        pagina.Instance.NovaTentativa();
        await pagina.InvokeAsync(pagina.Instance.DoarAsync);

        var chaves = _campanhas.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IClienteCampanhas.DoarAsync)).Select(c => (string)c.GetArguments()[2]!).ToList();
        chaves.Count.ShouldBe(3);
        chaves[0].ShouldBe(chaveInicial);
        chaves[1].ShouldBe(chaveInicial);
        chaves[2].ShouldNotBe(chaveInicial);
    }

    /// <summary>
    /// Dado um envio em andamento, quando o botão é clicado de novo, então o segundo clique é ignorado e o botão fica desabilitado.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Doar_EnvioEmAndamento_BotaoDesabilitadoEClique2Ignorado()
    {
        var liberar = new TaskCompletionSource<ResultadoApi<DoacaoAceita>>();
        _campanhas.DoarAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(liberar.Task);
        var pagina = Render<Doar>(p => p.Add(c => c.Id, _campanha));
        pagina.Instance.Modelo.Valor = 10m;

        var primeiro = pagina.InvokeAsync(pagina.Instance.DoarAsync);
        pagina.Render();
        await pagina.InvokeAsync(pagina.Instance.DoarAsync);

        pagina.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
        pagina.Find("button[type='submit']").TextContent.ShouldBe("Enviando…");
        liberar.SetResult(ResultadoApi.Ok(new DoacaoAceita(Guid.NewGuid(), "Recebida", "ok")));
        await primeiro;
        await _campanhas.Received(1).DoarAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dada uma recusa da API, quando a doação é enviada, então a mensagem do manual aparece e o formulário continua.
    /// </summary>
    /// <returns>Tarefa que termina com as verificações feitas.</returns>
    [Fact]
    public async Task Doar_CampanhaNaoAtiva_MostraMensagemDoManual()
    {
        _campanhas.DoarAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultadoApi.Falha<DoacaoAceita>(new ErroApi(422, "doacao.campanha_nao_ativa", "x", new Dictionary<string, string[]>(), null)));
        var pagina = Render<Doar>(p => p.Add(c => c.Id, _campanha));
        pagina.Instance.Modelo.Valor = 10m;

        await pagina.InvokeAsync(pagina.Instance.DoarAsync);
        pagina.Render();

        pagina.Find("[data-teste='mensagens'] li").TextContent.ShouldBe("A campanha não está aceitando doações.");
        pagina.Find("button[type='submit']").TextContent.ShouldBe("Confirmar doação");
    }

    /// <summary>
    /// Dado o modelo do formulário, quando validado, então o valor precisa ficar entre R$ 0,01 e R$ 1.000.000,00.
    /// </summary>
    /// <param name="valor">Valor testado.</param>
    /// <param name="validos">Se deve ser válido.</param>
    [Theory]
    [InlineData("0", false)]
    [InlineData("0.01", true)]
    [InlineData("150.50", true)]
    [InlineData("1000000.00", true)]
    [InlineData("1000000.01", false)]
    [InlineData("-5", false)]
    public void Modelo_Valor_SegueOManual(string valor, bool validos)
    {
        var modelo = new Doar.ModeloDeDoacao { Valor = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) };

        Validator.TryValidateObject(modelo, new ValidationContext(modelo), [], validateAllProperties: true).ShouldBe(validos);
    }
}
