using System.ComponentModel.DataAnnotations;
using ConexaoSolidaria.Web.Servicos;

namespace ConexaoSolidaria.Web.Components.Pages.Gestao;

/// <summary>
/// Dados do formulário de criação e edição de campanha, com as regras de tamanho e valor do manual. A API valida de novo.
/// </summary>
public sealed class ModeloDeCampanha : IValidatableObject
{
    /// <summary>
    /// Obtém ou define o título.
    /// </summary>
    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O título deve ter de 3 a 120 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a descrição.
    /// </summary>
    [StringLength(2000, ErrorMessage = "A descrição deve ter até 2000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de início.
    /// </summary>
    public DateOnly DataInicio { get; set; }

    /// <summary>
    /// Obtém ou define a data de término.
    /// </summary>
    public DateOnly DataFim { get; set; }

    /// <summary>
    /// Obtém ou define a meta financeira, em reais.
    /// </summary>
    [Range(0.01, 99999999.99, ErrorMessage = "A meta deve ser maior que zero e ter no máximo R$ 99.999.999,99.")]
    public decimal MetaFinanceira { get; set; }

    /// <summary>
    /// Cria um modelo preenchido com uma campanha existente.
    /// </summary>
    /// <param name="campanha">Campanha lida da API.</param>
    /// <returns>O modelo preenchido.</returns>
    public static ModeloDeCampanha De(CampanhaGestao campanha)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        return new ModeloDeCampanha
        {
            Titulo = campanha.Titulo,
            Descricao = campanha.Descricao,
            DataInicio = campanha.DataInicio,
            DataFim = campanha.DataFim,
            MetaFinanceira = campanha.MetaFinanceira,
        };
    }

    /// <summary>
    /// Converte o modelo no corpo enviado à API.
    /// </summary>
    /// <returns>O pedido de criação ou edição.</returns>
    public CampanhaRequest ParaRequisicao()
    {
        return new CampanhaRequest(Titulo.Trim(), Descricao.Trim(), DataInicio, DataFim, MetaFinanceira);
    }

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataFim < DataInicio)
        {
            yield return new ValidationResult("A data de término não pode ser anterior à data de início.", [nameof(DataFim)]);
        }
    }
}
