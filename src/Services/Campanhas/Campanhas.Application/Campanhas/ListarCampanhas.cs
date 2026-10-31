using Campanhas.Application.Abstracoes;
using Campanhas.Application.Comum;
using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;
using FluentValidation;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Filtros e paginação da listagem de campanhas.
/// </summary>
/// <param name="Status">Status a filtrar (<c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>); nulo traz todos.</param>
/// <param name="Page">Número da página, a partir de 1.</param>
/// <param name="PageSize">Itens por página, de 1 a 100.</param>
public sealed record ListarCampanhasRequest(string? Status = null, int Page = 1, int PageSize = 20);

/// <summary>
/// Página de campanhas devolvida pela API de gestão.
/// </summary>
/// <param name="Items">Campanhas da página, ordenadas pela data fim.</param>
/// <param name="Page">Número da página.</param>
/// <param name="PageSize">Itens por página.</param>
/// <param name="TotalItems">Total de campanhas que passam no filtro.</param>
/// <param name="TotalPages">Total de páginas.</param>
public sealed record PaginaDeCampanhasResponse(IReadOnlyList<CampanhaResponse> Items, int Page, int PageSize, int TotalItems, int TotalPages);

/// <summary>
/// Valida a página, o tamanho da página e o filtro de status.
/// </summary>
public sealed class ListarCampanhasRequestValidator : AbstractValidator<ListarCampanhasRequest>
{
    /// <summary>
    /// Maior tamanho de página aceito.
    /// </summary>
    public const int TamanhoMaximoDePagina = 100;

    /// <summary>
    /// Inicializa as regras.
    /// </summary>
    public ListarCampanhasRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, TamanhoMaximoDePagina).WithMessage("O tamanho da página deve ficar entre 1 e 100.");
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<StatusCampanha>(status, ignoreCase: false, out var valor) && Enum.IsDefined(valor))
            .WithMessage("Status desconhecido. Use Ativa, Concluida ou Cancelada.")
            .When(x => x.Status is not null, ApplyConditionTo.CurrentValidator);
    }
}

/// <summary>
/// Caso de uso de listagem de campanhas para o gestor, com filtro por status, ordenação pela data fim e paginação.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
/// <param name="validador">Validador do pedido.</param>
public sealed class ListarCampanhas(ICampanhaRepositorio campanhas, IValidator<ListarCampanhasRequest> validador)
{
    /// <summary>
    /// Executa a listagem.
    /// </summary>
    /// <param name="request">Filtros e paginação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página pedida ou o erro <c>validacao</c>.</returns>
    public async Task<Result<PaginaDeCampanhasResponse>> ExecutarAsync(ListarCampanhasRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<PaginaDeCampanhasResponse>(ErrosDeValidacao.Converter(validacao));
        }

        StatusCampanha? status = request.Status is null ? null : Enum.Parse<StatusCampanha>(request.Status);
        var pagina = await campanhas.ListarAsync(status, request.Page, request.PageSize, cancelamento);
        var totalDePaginas = (int)Math.Ceiling(pagina.Total / (double)request.PageSize);

        return Result.Sucesso(new PaginaDeCampanhasResponse(
            [.. pagina.Itens.Select(CampanhaResponse.De)],
            request.Page,
            request.PageSize,
            pagina.Total,
            totalDePaginas));
    }
}

/// <summary>
/// Caso de uso de detalhe de uma campanha para o gestor.
/// </summary>
/// <param name="campanhas">Repositório de campanhas.</param>
public sealed class ObterCampanha(ICampanhaRepositorio campanhas)
{
    /// <summary>
    /// Executa a consulta.
    /// </summary>
    /// <param name="id">Identificador da campanha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A campanha ou <c>campanha.nao_encontrada</c>.</returns>
    public async Task<Result<CampanhaResponse>> ExecutarAsync(CampanhaId id, CancellationToken cancelamento)
    {
        var campanha = await campanhas.ObterPorIdAsync(id, cancelamento);
        return campanha is null
            ? Result.Falha<CampanhaResponse>(ErrosDeCampanha.NaoEncontrada)
            : Result.Sucesso(CampanhaResponse.De(campanha));
    }
}
