using Campanhas.Api.Endpoints;
using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Comum;
using Campanhas.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.UnitTests.Persistencia;

/// <summary>
/// Testes da tradução de erros de negócio em respostas HTTP e do registro dos serviços de infraestrutura de Campanhas.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ApiETestesDeRegistroTests
{
    /// <summary>
    /// Dado cada tipo de erro de negócio, quando o status é escolhido, então segue a tabela do contrato HTTP.
    /// </summary>
    /// <param name="codigo">Código do erro.</param>
    /// <param name="status">Status esperado.</param>
    [Theory]
    [InlineData("validacao", 400)]
    [InlineData("campanha.nao_encontrada", 404)]
    [InlineData("campanha.conflito_de_versao", 409)]
    [InlineData("campanha.versao_obrigatoria", 412)]
    [InlineData("campanha.data_fim_passada", 422)]
    [InlineData("campanha.meta_invalida", 422)]
    [InlineData("campanha.nao_editavel", 422)]
    [InlineData("campanha.transicao_invalida", 422)]
    public void StatusDe_CadaErro_SegueOContrato(string codigo, int status)
    {
        ProblemasHttp.StatusDe(new Erro(codigo, "m")).ShouldBe(status);
    }

    /// <summary>
    /// Dado um erro de validação, quando convertido, então vira 400 com os campos; um erro de regra vira o status e o código escolhidos.
    /// </summary>
    [Fact]
    public void Para_ErroDeValidacaoEDeRegra_GeraRespostasCertas()
    {
        var validacao = new Erro(ErrosDeValidacao.CodigoValidacao, "x") { Campos = new Dictionary<string, string[]> { ["titulo"] = ["obrigatório"] } };

        var resposta400 = ProblemasHttp.Para(validacao).ShouldBeOfType<ValidationProblem>();
        var resposta422 = ProblemasHttp.Para(new Erro("campanha.meta_invalida", "Meta inválida.")).ShouldBeOfType<ProblemHttpResult>();

        resposta400.ProblemDetails.Errors["titulo"].ShouldBe(["obrigatório"]);
        resposta422.StatusCode.ShouldBe(422);
        resposta422.ProblemDetails.Extensions["code"].ShouldBe("campanha.meta_invalida");
        Should.Throw<ArgumentNullException>(() => ProblemasHttp.Para(null!));
        Should.Throw<ArgumentNullException>(() => ProblemasHttp.StatusDe(null!));
    }

    /// <summary>
    /// Dada uma versão, quando formatada como ETag, então vem entre aspas.
    /// </summary>
    [Fact]
    public void ETag_Versao_VemEntreAspas()
    {
        ProblemasHttp.ETag(7).ShouldBe("\"7\"");
    }

    /// <summary>
    /// Dada a configuração completa, quando os serviços são registrados, então repositório e relógio são resolvidos; sem conexão, não sobe.
    /// </summary>
    [Fact]
    public void AddCampanhasInfraestrutura_ConfiguracaoCompletaOuAusente_RegistraOuFalha()
    {
        var completa = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Campanhas"] = "Host=localhost;Database=campanhas_db",
                ["RabbitMq:Host"] = "rabbit.local",
            })
            .Build();
        var servicos = new ServiceCollection().AddSingleton(TimeProvider.System).AddLogging();

        servicos.AddCampanhasInfraestrutura(completa);

        using var provedor = servicos.BuildServiceProvider();
        provedor.GetRequiredService<IRelogio>().ShouldNotBeNull();
        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddCampanhasInfraestrutura(new ConfigurationBuilder().Build()));
        Should.Throw<ArgumentNullException>(() => ExtensoesCampanhasInfraestrutura.AddCampanhasInfraestrutura(null!, completa));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddCampanhasInfraestrutura(null!));
    }
}
