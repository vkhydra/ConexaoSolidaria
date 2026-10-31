using Campanhas.Domain.Campanhas;
using Campanhas.Domain.Comum;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Converte os campos já validados de um <see cref="CampanhaRequest"/> nos value objects do domínio.
/// </summary>
/// <param name="Periodo">Período da campanha.</param>
/// <param name="Meta">Meta financeira.</param>
internal sealed record ValoresDaCampanha(Periodo Periodo, Dinheiro Meta)
{
    /// <summary>
    /// Monta o período e a meta. Meta negativa vira <c>campanha.meta_invalida</c>, como a meta zero que o agregado recusa.
    /// </summary>
    /// <param name="request">Pedido que passou pelo validador, então datas e meta existem.</param>
    /// <returns>Os valores ou o erro de regra (<c>campanha.periodo_invalido</c> ou <c>campanha.meta_invalida</c>).</returns>
    public static Result<ValoresDaCampanha> De(CampanhaRequest request)
    {
        var periodo = Periodo.Criar(request.DataInicio!.Value, request.DataFim!.Value);
        if (periodo.EhFalha)
        {
            return Result.Falha<ValoresDaCampanha>(periodo.Erro);
        }

        var meta = Dinheiro.Criar(request.MetaFinanceira!.Value);
        if (meta.EhFalha)
        {
            return Result.Falha<ValoresDaCampanha>(new Erro(Campanha.CodigoMetaInvalida, "A meta deve ser maior que zero e de até 99.999.999,99."));
        }

        return Result.Sucesso(new ValoresDaCampanha(periodo.Valor, meta.Valor));
    }
}
