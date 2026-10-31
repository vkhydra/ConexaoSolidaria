using FluentValidation;
using Identity.Domain.Usuarios;

namespace Identity.Application.Autenticacao;

/// <summary>
/// Valida o formato do login antes de qualquer consulta ao banco.
/// </summary>
public sealed class AutenticarRequestValidator : AbstractValidator<AutenticarRequest>
{
    /// <summary>
    /// Tamanho máximo aceito para a senha, que protege o hasher de entradas gigantes.
    /// </summary>
    public const int TamanhoMaximoSenha = 128;

    /// <summary>
    /// Inicializa as regras do e-mail e da senha.
    /// </summary>
    public AutenticarRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .Must(email => Email.Criar(email).EhSucesso).WithMessage("O e-mail informado não é válido.")
            .When(x => !string.IsNullOrEmpty(x.Email), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MaximumLength(TamanhoMaximoSenha).WithMessage("A senha é longa demais.");
    }
}
