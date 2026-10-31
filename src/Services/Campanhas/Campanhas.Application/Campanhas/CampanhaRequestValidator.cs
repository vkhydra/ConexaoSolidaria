using Campanhas.Domain.Campanhas;
using FluentValidation;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Valida o formato dos dados da campanha antes das regras de negócio. Valores fora da regra (meta zero ou negativa, data fim
/// no passado) passam por aqui e são recusados pelo domínio com código próprio (422).
/// </summary>
public sealed class CampanhaRequestValidator : AbstractValidator<CampanhaRequest>
{
    /// <summary>
    /// Inicializa as regras de cada campo.
    /// </summary>
    public CampanhaRequestValidator()
    {
        RuleFor(x => x.Titulo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .Must(t => t == t!.Trim()).WithMessage("O título não pode ter espaços no início ou no fim.")
            .Length(Campanha.TituloTamanhoMinimo, Campanha.TituloTamanhoMaximo).WithMessage("O título deve ter de 3 a 120 caracteres.");

        RuleFor(x => x.Descricao)
            .MaximumLength(Campanha.DescricaoTamanhoMaximo).WithMessage("A descrição deve ter até 2000 caracteres.");

        RuleFor(x => x.DataInicio).NotNull().WithMessage("A data início é obrigatória.");
        RuleFor(x => x.DataFim).NotNull().WithMessage("A data fim é obrigatória.");

        RuleFor(x => x.MetaFinanceira)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A meta financeira é obrigatória.")
            .Must(meta => decimal.Round(meta!.Value, 2) == meta.Value).WithMessage("A meta deve ter no máximo duas casas decimais.");
    }
}
