using Campanhas.Domain.Comum;

namespace Campanhas.Domain.Campanhas;

/// <summary>
/// Raiz do agregado de campanha de arrecadação: título, período, meta e situação. O total arrecadado nunca é alterado
/// por este agregado; só o Arrecadacao.Worker o escreve.
/// </summary>
public sealed class Campanha
{
    /// <summary>
    /// Código do erro de título inválido.
    /// </summary>
    public const string CodigoTituloInvalido = "campanha.titulo_invalido";

    /// <summary>
    /// Código do erro de descrição inválida.
    /// </summary>
    public const string CodigoDescricaoInvalida = "campanha.descricao_invalida";

    /// <summary>
    /// Código do erro de data fim no passado.
    /// </summary>
    public const string CodigoDataFimPassada = "campanha.data_fim_passada";

    /// <summary>
    /// Código do erro de meta fora do intervalo aceito.
    /// </summary>
    public const string CodigoMetaInvalida = "campanha.meta_invalida";

    /// <summary>
    /// Tamanho mínimo do título.
    /// </summary>
    public const int TituloTamanhoMinimo = 3;

    /// <summary>
    /// Tamanho máximo do título.
    /// </summary>
    public const int TituloTamanhoMaximo = 120;

    /// <summary>
    /// Tamanho máximo da descrição.
    /// </summary>
    public const int DescricaoTamanhoMaximo = 2000;

    /// <summary>
    /// Maior meta aceita, que cabe com folga na coluna <c>numeric(18,2)</c>.
    /// </summary>
    public const decimal MetaMaxima = 99_999_999.99m;

    /// <summary>
    /// Eventos de domínio ocorridos desde o último <see cref="LimparEventos"/>.
    /// </summary>
    private readonly List<IEventoDeDominio> _eventos = [];

    /// <summary>
    /// Construtor sem parâmetros usado pelo EF Core para materializar o agregado.
    /// </summary>
    private Campanha()
    {
        Titulo = string.Empty;
        Descricao = string.Empty;
        Periodo = null!;
        MetaFinanceira = null!;
        TotalArrecadado = null!;
    }

    /// <summary>
    /// Cria uma campanha com todos os dados.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="titulo">Título.</param>
    /// <param name="descricao">Descrição.</param>
    /// <param name="periodo">Período.</param>
    /// <param name="meta">Meta financeira.</param>
    /// <param name="agora">Instante da criação.</param>
    private Campanha(CampanhaId id, string titulo, string descricao, Periodo periodo, Dinheiro meta, DateTimeOffset agora)
    {
        Id = id;
        Titulo = titulo;
        Descricao = descricao;
        Periodo = periodo;
        MetaFinanceira = meta;
        TotalArrecadado = Dinheiro.Zero;
        Status = StatusCampanha.Ativa;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>
    /// Obtém o identificador da campanha.
    /// </summary>
    public CampanhaId Id { get; private set; }

    /// <summary>
    /// Obtém o título, de 3 a 120 caracteres e sem espaços nas pontas.
    /// </summary>
    public string Titulo { get; private set; }

    /// <summary>
    /// Obtém a descrição, de até 2000 caracteres.
    /// </summary>
    public string Descricao { get; private set; }

    /// <summary>
    /// Obtém o período da campanha.
    /// </summary>
    public Periodo Periodo { get; private set; }

    /// <summary>
    /// Obtém a meta financeira.
    /// </summary>
    public Dinheiro MetaFinanceira { get; private set; }

    /// <summary>
    /// Obtém o total arrecadado, que começa em zero e só o Arrecadacao.Worker altera.
    /// </summary>
    public Dinheiro TotalArrecadado { get; private set; }

    /// <summary>
    /// Obtém a situação da campanha.
    /// </summary>
    public StatusCampanha Status { get; private set; }

    /// <summary>
    /// Obtém a data e a hora da criação.
    /// </summary>
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Obtém a data e a hora da última alteração.
    /// </summary>
    public DateTimeOffset AtualizadoEm { get; private set; }

    /// <summary>
    /// Obtém a versão da linha (<c>xmin</c> do PostgreSQL), usada como token de concorrência e como ETag.
    /// </summary>
    public uint Versao { get; private set; }

    /// <summary>
    /// Obtém os eventos de domínio ainda não publicados.
    /// </summary>
    public IReadOnlyList<IEventoDeDominio> Eventos => _eventos;

    /// <summary>
    /// Cria uma campanha ativa, com total arrecadado zero, e registra o evento <see cref="CampanhaCriada"/>.
    /// </summary>
    /// <param name="titulo">Título, de 3 a 120 caracteres e sem espaços nas pontas.</param>
    /// <param name="descricao">Descrição, de até 2000 caracteres.</param>
    /// <param name="periodo">Período da campanha.</param>
    /// <param name="meta">Meta financeira, maior que zero e de até 99.999.999,99.</param>
    /// <param name="hoje">Data de hoje em São Paulo; a data fim não pode ser anterior a ela.</param>
    /// <param name="agora">Instante da criação.</param>
    /// <returns>A campanha ou o primeiro erro de regra encontrado.</returns>
    public static Result<Campanha> Criar(string? titulo, string? descricao, Periodo periodo, Dinheiro meta, DateOnly hoje, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(meta);

        var erro = ValidarDados(titulo, descricao, periodo, meta, hoje);
        if (erro is not null)
        {
            return Result.Falha<Campanha>(erro);
        }

        var campanha = new Campanha(CampanhaId.Novo(), titulo!, descricao!, periodo, meta, agora);
        campanha._eventos.Add(new CampanhaCriada(campanha.Id, agora));
        return Result.Sucesso(campanha);
    }

    /// <summary>
    /// Esvazia a lista de eventos de domínio, depois que foram publicados.
    /// </summary>
    public void LimparEventos()
    {
        _eventos.Clear();
    }

    /// <summary>
    /// Confere as regras de título, descrição, período e meta, na ordem em que as mensagens ajudam mais o gestor.
    /// </summary>
    /// <param name="titulo">Título informado.</param>
    /// <param name="descricao">Descrição informada.</param>
    /// <param name="periodo">Período informado.</param>
    /// <param name="meta">Meta informada.</param>
    /// <param name="hoje">Data de hoje em São Paulo.</param>
    /// <returns>O primeiro erro encontrado ou nulo se tudo está certo.</returns>
    private static Erro? ValidarDados(string? titulo, string? descricao, Periodo periodo, Dinheiro meta, DateOnly hoje)
    {
        if (titulo is null || titulo.Length is < TituloTamanhoMinimo or > TituloTamanhoMaximo || titulo != titulo.Trim())
        {
            return new Erro(CodigoTituloInvalido, "O título deve ter de 3 a 120 caracteres, sem espaços nas pontas.");
        }

        if (descricao is null || descricao.Length > DescricaoTamanhoMaximo)
        {
            return new Erro(CodigoDescricaoInvalida, "A descrição deve ter até 2000 caracteres.");
        }

        if (periodo.DataFim < hoje)
        {
            return new Erro(CodigoDataFimPassada, "A data fim não pode ser anterior a hoje.");
        }

        if (meta.Valor <= 0m || meta.Valor > MetaMaxima)
        {
            return new Erro(CodigoMetaInvalida, "A meta deve ser maior que zero e de até 99.999.999,99.");
        }

        return null;
    }
}
