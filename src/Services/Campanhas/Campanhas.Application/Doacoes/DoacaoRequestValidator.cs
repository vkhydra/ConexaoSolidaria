using FluentValidation;

namespace Campanhas.Application.Doacoes;

/// <summary>
/// Valida o formato do pedido de doação antes das regras de negócio.
/// </summary>
public sealed class DoacaoRequestValidator : AbstractValidator<DoacaoRequest>
{
    /// <summary>
    /// Maior valor aceito em uma doação.
    /// </summary>
    public const decimal ValorMaximo = 1_000_000.00m;

    /// <summary>
    /// Inicializa as regras de cada campo.
    /// </summary>
    public DoacaoRequestValidator()
    {
        RuleFor(x => x.IdCampanha)
            .Must(id => id.HasValue && id.Value != Guid.Empty).WithMessage("Informe a campanha da doação.");

        RuleFor(x => x.ValorDoacao)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("O valor da doação é obrigatório.")
            .GreaterThan(0m).WithMessage("O valor da doação deve ser maior que zero.")
            .Must(valor => decimal.Round(valor!.Value, 2) == valor.Value).WithMessage("O valor da doação deve ter no máximo duas casas decimais.")
            .LessThanOrEqualTo(ValorMaximo).WithMessage("O valor da doação deve ser de até 1.000.000,00.");
    }
}
