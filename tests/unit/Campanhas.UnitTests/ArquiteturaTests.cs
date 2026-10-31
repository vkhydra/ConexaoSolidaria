using System.Reflection;
using Campanhas.UnitTests.Fixtures;
using NetArchTest.Rules;

namespace Campanhas.UnitTests;

/// <summary>
/// Regras de arquitetura do contexto de Campanhas: direção das dependências entre as camadas
/// e isolamento em relação aos outros contextos.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArquiteturaTests
{
    /// <summary>
    /// Prefixos de infraestrutura que o domínio e a aplicação não podem referenciar.
    /// </summary>
    private static readonly string[] _infraestrutura =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MassTransit",
        "Npgsql",
        "MongoDB",
        "StackExchange.Redis",
        "Elastic",
    ];

    /// <summary>
    /// Prefixos dos assemblies de outros contextos, que Campanhas não pode referenciar.
    /// </summary>
    private static readonly string[] _outrosContextos = ["Identity.", "Notificacoes."];

    /// <summary>
    /// Dado o domínio de Campanhas, quando as dependências são analisadas, então não há infraestrutura.
    /// </summary>
    [Fact]
    public void Dominio_NaoDependeDeInfraestrutura()
    {
        var assembly = Assembly.Load("Campanhas.Domain");

        var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(_infraestrutura).GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Falhas(resultado));
        ReferenciasDe(assembly).ShouldNotContain(nome => _infraestrutura.Any(p => nome.StartsWith(p, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Dada a aplicação de Campanhas, quando as dependências são analisadas, então não há infraestrutura nem API.
    /// </summary>
    [Fact]
    public void Aplicacao_NaoDependeDeInfraestruturaNemDaApi()
    {
        var assembly = Assembly.Load("Campanhas.Application");
        string[] proibidos = ["Campanhas.Infrastructure", "Campanhas.Api", .. _infraestrutura];

        var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(proibidos).GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Falhas(resultado));
        ReferenciasDe(assembly).ShouldNotContain(nome => proibidos.Any(p => nome.StartsWith(p, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Dada a infraestrutura de Campanhas, quando as dependências são analisadas, então não há dependência da API.
    /// </summary>
    [Fact]
    public void Infraestrutura_NaoDependeDaApi()
    {
        var assembly = Assembly.Load("Campanhas.Infrastructure");

        var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn("Campanhas.Api").GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Falhas(resultado));
        ReferenciasDe(assembly).ShouldNotContain("Campanhas.Api");
    }

    /// <summary>
    /// Dados todos os projetos de Campanhas, quando as referências são analisadas, então nenhum usa outro contexto.
    /// </summary>
    /// <param name="projeto">Nome do assembly de Campanhas.</param>
    [Theory]
    [InlineData("Campanhas.Domain")]
    [InlineData("Campanhas.Application")]
    [InlineData("Campanhas.Infrastructure")]
    [InlineData("Campanhas.Api")]
    public void Contexto_NaoReferenciaOutrosContextos(string projeto)
    {
        var referencias = ReferenciasDe(Assembly.Load(projeto));

        referencias.ShouldNotContain(nome => _outrosContextos.Any(p => nome.StartsWith(p, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Dado um tipo que usa uma dependência proibida, quando a regra é aplicada, então a violação é acusada.
    /// </summary>
    [Fact]
    public void Regra_TipoComDependenciaProibida_AcusaViolacao()
    {
        var resultado = Types.InAssembly(typeof(ClienteProibido).Assembly)
            .That().HaveName(nameof(ClienteProibido))
            .ShouldNot().HaveDependencyOn("System.Net.Http")
            .GetResult();

        resultado.IsSuccessful.ShouldBeFalse();
        resultado.FailingTypeNames.ShouldContain(typeof(ClienteProibido).FullName!);
    }

    /// <summary>
    /// Dado um tipo sem a dependência proibida, quando a regra é aplicada, então ele é aceito.
    /// </summary>
    [Fact]
    public void Regra_TipoSemDependenciaProibida_AceitaTipo()
    {
        var resultado = Types.InAssembly(typeof(ClientePermitido).Assembly)
            .That().HaveName(nameof(ClientePermitido))
            .ShouldNot().HaveDependencyOn("System.Net.Http")
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(Falhas(resultado));
    }

    /// <summary>
    /// Lista os nomes dos assemblies referenciados.
    /// </summary>
    /// <param name="assembly">Assembly analisado.</param>
    /// <returns>Nomes dos assemblies referenciados.</returns>
    private static List<string> ReferenciasDe(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
    }

    /// <summary>
    /// Monta a mensagem com os tipos que violaram a regra.
    /// </summary>
    /// <param name="resultado">Resultado da regra.</param>
    /// <returns>Texto com os nomes dos tipos que falharam.</returns>
    private static string Falhas(NetArchTest.Rules.TestResult resultado)
    {
        return "Tipos em violação: " + string.Join(", ", resultado.FailingTypeNames ?? []);
    }
}
