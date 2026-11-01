using Campanhas.Application.Comum;
using Campanhas.Domain.Comum;
using FluentValidation;

namespace Campanhas.Application.Busca;

/// <summary>
/// Texto e paginação da busca pública.
/// </summary>
/// <param name="Q">Texto procurado, de 2 a 100 caracteres.</param>
/// <param name="Page">Número da página, a partir de 1.</param>
/// <param name="PageSize">Itens por página, de 1 a 50.</param>
public sealed record BuscarCampanhasRequest(string? Q, int Page = 1, int PageSize = 10);

/// <summary>
/// Valida o texto procurado, a página e o tamanho da página.
/// </summary>
public sealed class BuscarCampanhasRequestValidator : AbstractValidator<BuscarCampanhasRequest>
{
    /// <summary>
    /// Menor tamanho do texto, sem contar os espaços das pontas.
    /// </summary>
    public const int TamanhoMinimoDoTexto = 2;

    /// <summary>
    /// Maior tamanho do texto, sem contar os espaços das pontas.
    /// </summary>
    public const int TamanhoMaximoDoTexto = 100;

    /// <summary>
    /// Maior tamanho de página aceito.
    /// </summary>
    public const int TamanhoMaximoDePagina = 50;

    /// <summary>
    /// Inicializa as regras.
    /// </summary>
    public BuscarCampanhasRequestValidator()
    {
        RuleFor(x => x.Q)
            .Must(q => q is not null && q.Trim().Length is >= TamanhoMinimoDoTexto and <= TamanhoMaximoDoTexto)
            .WithMessage($"O texto da busca deve ter entre {TamanhoMinimoDoTexto} e {TamanhoMaximoDoTexto} caracteres.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, TamanhoMaximoDePagina).WithMessage("O tamanho da página deve ficar entre 1 e 50.");
    }
}

/// <summary>
/// Caso de uso da busca pública de campanhas ativas.
/// </summary>
/// <param name="busca">Porta de consulta ao índice.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="metricas">Métricas da busca.</param>
public sealed class BuscarCampanhas(IBuscaCampanhas busca, IValidator<BuscarCampanhasRequest> validador, IMetricasBusca metricas)
{
    /// <summary>
    /// Executa a busca.
    /// </summary>
    /// <param name="request">Texto e paginação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A página de resultados (que pode ser vazia), o erro <c>validacao</c> ou o erro <c>busca.indisponivel</c>.</returns>
    public async Task<Result<BuscaResponse>> ExecutarAsync(BuscarCampanhasRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<BuscaResponse>(ErrosDeValidacao.Converter(validacao));
        }

        try
        {
            var pagina = await busca.BuscarAsync(request.Q!.Trim(), request.Page, request.PageSize, cancelamento);
            var totalDePaginas = (int)Math.Ceiling(pagina.Total / (double)request.PageSize);
            metricas.RegistrarBusca(pagina.Total > 0 ? ResultadoDaBusca.Ok : ResultadoDaBusca.Vazio);
            return Result.Sucesso(new BuscaResponse(pagina.Itens, request.Page, request.PageSize, pagina.Total, totalDePaginas));
        }
        catch (BuscaIndisponivelException)
        {
            metricas.RegistrarBusca(ResultadoDaBusca.Indisponivel);
            return Result.Falha<BuscaResponse>(ErrosDeBusca.Indisponivel);
        }
    }
}
