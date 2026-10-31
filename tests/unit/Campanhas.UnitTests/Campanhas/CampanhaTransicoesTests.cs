using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;

namespace Campanhas.UnitTests.Campanhas;

/// <summary>
/// Testes da edição e das transições de status do agregado <see cref="Campanha"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CampanhaTransicoesTests
{
    /// <summary>
    /// Data de hoje usada nos cenários.
    /// </summary>
    private static readonly DateOnly _hoje = new(2026, 11, 10);

    /// <summary>
    /// Instante da criação.
    /// </summary>
    private static readonly DateTimeOffset _criacao = new(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Instante das alterações.
    /// </summary>
    private static readonly DateTimeOffset _alteracao = _criacao.AddHours(2);

    /// <summary>
    /// Dada uma campanha ativa, quando é atualizada com dados válidos, então os dados mudam, o total fica e o evento é registrado.
    /// </summary>
    [Fact]
    public void Atualizar_CampanhaAtiva_AlteraDadosEMantemTotal()
    {
        var campanha = NovaCampanha();
        campanha.LimparEventos();

        var resultado = campanha.Atualizar("Natal 2026", "Nova descrição", Periodo(_hoje, new DateOnly(2027, 1, 5)), Meta(12000m), _hoje, _alteracao);

        resultado.EhSucesso.ShouldBeTrue();
        campanha.Titulo.ShouldBe("Natal 2026");
        campanha.Descricao.ShouldBe("Nova descrição");
        campanha.Periodo.DataFim.ShouldBe(new DateOnly(2027, 1, 5));
        campanha.MetaFinanceira.Valor.ShouldBe(12000m);
        campanha.TotalArrecadado.ShouldBe(Dinheiro.Zero);
        campanha.AtualizadoEm.ShouldBe(_alteracao);
        campanha.CriadoEm.ShouldBe(_criacao);
        campanha.Eventos.ShouldHaveSingleItem().ShouldBe(new CampanhaAtualizada(campanha.Id, _alteracao));
    }

    /// <summary>
    /// Dada uma campanha ativa, quando a atualização viola uma regra, então devolve o erro e nada muda.
    /// </summary>
    [Fact]
    public void Atualizar_DadosInvalidos_DevolveErroENaoAltera()
    {
        var campanha = NovaCampanha();
        campanha.LimparEventos();

        var resultado = campanha.Atualizar("Natal", string.Empty, Periodo(_hoje, _hoje), Meta(0m), _hoje, _alteracao);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoMetaInvalida);
        campanha.MetaFinanceira.Valor.ShouldBe(10000m);
        campanha.AtualizadoEm.ShouldBe(_criacao);
        campanha.Eventos.ShouldBeEmpty();
    }

    /// <summary>
    /// Dada uma campanha concluída ou cancelada, quando é atualizada, então devolve campanha.nao_editavel.
    /// </summary>
    /// <param name="concluir">Se a campanha foi concluída (senão, cancelada).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Atualizar_CampanhaNaoAtiva_DevolveNaoEditavel(bool concluir)
    {
        var campanha = NovaCampanha();
        _ = concluir ? campanha.Concluir(_alteracao) : campanha.Cancelar(_alteracao);

        var resultado = campanha.Atualizar("Natal 2026", string.Empty, Periodo(_hoje, _hoje), Meta(1m), _hoje, _alteracao);

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoNaoEditavel);
    }

    /// <summary>
    /// Dados argumentos nulos, quando a campanha é atualizada, então a chamada é recusada.
    /// </summary>
    [Fact]
    public void Atualizar_ArgumentosNulos_LancaExcecao()
    {
        var campanha = NovaCampanha();

        Should.Throw<ArgumentNullException>(() => campanha.Atualizar("Natal", string.Empty, null!, Meta(1m), _hoje, _alteracao));
        Should.Throw<ArgumentNullException>(() => campanha.Atualizar("Natal", string.Empty, Periodo(_hoje, _hoje), null!, _hoje, _alteracao));
    }

    /// <summary>
    /// Dada uma campanha ativa, quando é concluída, então muda o status e registra o evento.
    /// </summary>
    [Fact]
    public void Concluir_CampanhaAtiva_MudaStatusERegistraEvento()
    {
        var campanha = NovaCampanha();
        campanha.LimparEventos();

        var resultado = campanha.Concluir(_alteracao);

        resultado.EhSucesso.ShouldBeTrue();
        campanha.Status.ShouldBe(StatusCampanha.Concluida);
        campanha.AtualizadoEm.ShouldBe(_alteracao);
        campanha.Eventos.ShouldHaveSingleItem().ShouldBe(new CampanhaConcluida(campanha.Id, _alteracao));
    }

    /// <summary>
    /// Dada uma campanha ativa, quando é cancelada, então muda o status e registra o evento.
    /// </summary>
    [Fact]
    public void Cancelar_CampanhaAtiva_MudaStatusERegistraEvento()
    {
        var campanha = NovaCampanha();
        campanha.LimparEventos();

        var resultado = campanha.Cancelar(_alteracao);

        resultado.EhSucesso.ShouldBeTrue();
        campanha.Status.ShouldBe(StatusCampanha.Cancelada);
        campanha.Eventos.ShouldHaveSingleItem().ShouldBe(new CampanhaCancelada(campanha.Id, _alteracao));
    }

    /// <summary>
    /// Dada uma campanha que já saiu de ativa, quando se tenta concluir ou cancelar de novo, então devolve campanha.transicao_invalida e nada muda.
    /// </summary>
    /// <param name="primeiraConcluir">Se a primeira transição foi concluir (senão, cancelar).</param>
    /// <param name="segundaConcluir">Se a segunda tentativa é concluir (senão, cancelar).</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Transicao_ForaDeAtiva_DevolveTransicaoInvalida(bool primeiraConcluir, bool segundaConcluir)
    {
        var campanha = NovaCampanha();
        _ = primeiraConcluir ? campanha.Concluir(_alteracao) : campanha.Cancelar(_alteracao);
        var statusAntes = campanha.Status;
        campanha.LimparEventos();

        var resultado = segundaConcluir ? campanha.Concluir(_alteracao.AddHours(1)) : campanha.Cancelar(_alteracao.AddHours(1));

        resultado.Erro.Codigo.ShouldBe(Campanha.CodigoTransicaoInvalida);
        campanha.Status.ShouldBe(statusAntes);
        campanha.AtualizadoEm.ShouldBe(_alteracao);
        campanha.Eventos.ShouldBeEmpty();
    }

    /// <summary>
    /// Cria uma campanha ativa de 10000.00 para os testes.
    /// </summary>
    /// <returns>A campanha criada.</returns>
    private static Campanha NovaCampanha()
    {
        return Campanha.Criar("Natal Solidário", "Cestas", Periodo(_hoje, new DateOnly(2026, 12, 24)), Meta(10000m), _hoje, _criacao).Valor;
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
    /// Cria um valor em dinheiro para os testes.
    /// </summary>
    /// <param name="valor">Valor em reais.</param>
    /// <returns>O dinheiro.</returns>
    private static Dinheiro Meta(decimal valor)
    {
        return Dinheiro.Criar(valor).Valor;
    }
}
