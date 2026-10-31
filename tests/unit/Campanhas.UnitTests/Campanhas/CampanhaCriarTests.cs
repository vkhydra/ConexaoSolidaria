using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes da criação do agregado <see cref="Campanha"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CampanhaCriarTests
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Instante da criação usado nos cenários.
    /// </summary>
    private static readonly DateTimeOffset _agora = new(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Dados válidos, quando a campanha é criada, então nasce ativa, com total zero, e registra o evento de criação.
    /// </summary>
    [Fact]
    public void Criar_DadosValidos_CriaCampanhaAtivaComTotalZero()
    {
        var resultado = Campanha.Criar("Natal Solidário", "Cestas de Natal", Periodo(_hoje, new DateOnly(2026, 12, 24)), Meta(10000m), _hoje, _agora);

        resultado.EhSucesso.ShouldBeTrue();
        var campanha = resultado.Valor;
        campanha.Titulo.ShouldBe("Natal Solidário");
        campanha.Descricao.ShouldBe("Cestas de Natal");
        campanha.Status.ShouldBe(StatusCampanha.Ativa);
        campanha.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        campanha.MetaFinanceira.Valor.ShouldBe(10000m);
        campanha.CriadoEm.ShouldBe(_agora);
        campanha.AtualizadoEm.ShouldBe(_agora);
        campanha.Eventos.ShouldHaveSingleItem().ShouldBe(new CampanhaCriada(campanha.Id, _agora));
    }

    /// <summary>
    /// Dada uma campanha criada, quando os eventos são limpos, então a lista fica vazia.
    /// </summary>
    [Fact]
    public void LimparEventos_AposCriar_EsvaziaALista()
    {
        var campanha = Campanha.Criar("Natal", string.Empty, Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).Valor;

        campanha.LimparEventos();

        campanha.Eventos.ShouldBeEmpty();
    }

    /// <summary>
    /// Dada data fim igual a hoje, quando a campanha é criada, então é aceita; um dia antes é recusada.
    /// </summary>
    [Fact]
    public void Criar_DataFim_HojeAceitaEOntemRecusa()
    {
        Campanha.Criar("Natal", string.Empty, Periodo(new DateOnly(2026, 11, 1), _hoje), Meta(1m), _hoje, _agora).EhSucesso.ShouldBeTrue();

        var recusada = Campanha.Criar("Natal", string.Empty, Periodo(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 9)), Meta(1m), _hoje, _agora);

        recusada.Erro.Codigo.ShouldBe(Campanha.CodigoDataFimPassada);
    }

    /// <summary>
    /// Dada uma meta zero ou acima do limite, quando a campanha é criada, então devolve campanha.meta_invalida; o limite exato é aceito.
    /// </summary>
    /// <param name="meta">Meta informada.</param>
    /// <param name="valida">Se a meta deve ser aceita.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    [InlineData(99999999.99, true)]
    [InlineData(100000000.00, false)]
    public void Criar_Meta_AceitaSoDentroDoIntervalo(double meta, bool valida)
    {
        var resultado = Campanha.Criar("Natal", string.Empty, Periodo(_hoje, _hoje), Meta((decimal)meta), _hoje, _agora);

        resultado.EhSucesso.ShouldBe(valida);
        if (!valida)
        {
            resultado.Erro.Codigo.ShouldBe(Campanha.CodigoMetaInvalida);
        }
    }

    /// <summary>
    /// Dado um título ausente, curto, longo ou com espaços nas pontas, quando a campanha é criada, então devolve campanha.titulo_invalido.
    /// </summary>
    /// <param name="titulo">Título informado.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData(" Natal")]
    [InlineData("Natal ")]
    public void Criar_TituloInvalido_DevolveErro(string? titulo)
    {
        var resultado = Campanha.Criar(titulo, string.Empty, Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoTituloInvalido);
    }

    /// <summary>
    /// Dado um título com 121 caracteres, quando a campanha é criada, então é recusado; com 120 é aceito.
    /// </summary>
    [Fact]
    public void Criar_TituloNoLimite_AceitaCentoEVinteERecusaCentoEVinteEUm()
    {
        Campanha.Criar(new string('a', 120), string.Empty, Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).EhSucesso.ShouldBeTrue();
        Campanha.Criar(new string('a', 121), string.Empty, Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).Erro.Codigo.ShouldBe(Campanha.CodigoTituloInvalido);
    }

    /// <summary>
    /// Dada uma descrição ausente ou com mais de 2000 caracteres, quando a campanha é criada, então devolve campanha.descricao_invalida.
    /// </summary>
    [Fact]
    public void Criar_DescricaoInvalida_DevolveErro()
    {
        Campanha.Criar("Natal", new string('a', 2000), Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).EhSucesso.ShouldBeTrue();
        Campanha.Criar("Natal", new string('a', 2001), Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).Erro.Codigo.ShouldBe(Campanha.CodigoDescricaoInvalida);
        Campanha.Criar("Natal", null, Periodo(_hoje, _hoje), Meta(1m), _hoje, _agora).Erro.Codigo.ShouldBe(Campanha.CodigoDescricaoInvalida);
    }

    /// <summary>
    /// Dados argumentos nulos, quando a campanha é criada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Criar_ArgumentosNulos_LancaExcecao()
    {
        Should.Throw<ArgumentNullException>(() => Campanha.Criar("Natal", string.Empty, null!, Meta(1m), _hoje, _agora));
        Should.Throw<ArgumentNullException>(() => Campanha.Criar("Natal", string.Empty, Periodo(_hoje, _hoje), null!, _hoje, _agora));
    }

    /// <summary>
    /// Dois identificadores novos, quando comparados, então são diferentes.
    /// </summary>
    [Fact]
    public void CampanhaId_Novo_GeraIdentificadoresDiferentes()
    {
        var a = CampanhaId.Novo();

        a.ShouldNotBe(CampanhaId.Novo());
        a.ToString().ShouldBe(a.Valor.ToString());
    }

    /// <summary>
    /// Cria um período válido para os testes.
    /// </summary>
    /// <param name="inicio">Primeiro dia.</param>
    /// <param name="fim">Último dia.</param>
    /// <returns>O período.</returns>
    private static Periodo Periodo(DateOnly inicio, DateOnly fim)
    {
        return Domain.Comum.Periodo.Criar(inicio, fim).Valor;
    }

    /// <summary>
    /// Cria uma meta para os testes, sem passar pela regra de negativo.
    /// </summary>
    /// <param name="valor">Valor da meta.</param>
    /// <returns>O valor em dinheiro.</returns>
    private static Dinheiro Meta(decimal valor)
    {
        return Dinheiro.Criar(valor).Valor;
    }
}
