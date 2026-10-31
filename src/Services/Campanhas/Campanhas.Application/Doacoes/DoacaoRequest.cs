namespace Campanhas.Application.Doacoes;

/// <summary>
/// Pedido de doação enviado pelo doador autenticado.
/// </summary>
/// <param name="IdCampanha">Campanha que recebe a doação.</param>
/// <param name="ValorDoacao">Valor em reais, maior que zero, com até duas casas decimais e de até 1.000.000,00.</param>
public sealed record DoacaoRequest(Guid? IdCampanha, decimal? ValorDoacao);
