using FluentValidation;
using Identity.Domain.Usuarios;

namespace Identity.Application.Doadores;

/// <summary>
/// Valida o formato do cadastro antes de qualquer consulta. As mensagens não repetem o valor enviado.
/// </summary>
public sealed class CadastrarDoadorRequestValidator : AbstractValidator<CadastrarDoadorRequest>
{
    /// <summary>
    /// Inicializa as regras de cada campo.
    /// </summary>
    public CadastrarDoadorRequestValidator()
    {
        RuleFor(x => x.NomeCompleto)
            .NotEmpty().WithMessage("O nome completo é obrigatório.")
            .Must(NomeValido).WithMessage("Informe o nome completo, de 3 a 150 caracteres, com nome e sobrenome.")
            .When(x => !string.IsNullOrWhiteSpace(x.NomeCompleto), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .Must(email => Email.Criar(email).EhSucesso).WithMessage("O e-mail informado não é válido.")
            .When(x => !string.IsNullOrEmpty(x.Email), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Must(cpf => Cpf.Criar(cpf).EhSucesso).WithMessage("CPF inválido.")
            .When(x => !string.IsNullOrEmpty(x.Cpf), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .Must(senha => Senha.Criar(senha).EhSucesso).WithMessage(Senha.MensagemFraca)
            .When(x => !string.IsNullOrEmpty(x.Senha), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.AceitePoliticaPrivacidade)
            .Equal(true).WithMessage("É obrigatório aceitar a política de privacidade.");
    }

    /// <summary>
    /// Confere o tamanho do nome e se tem ao menos duas palavras.
    /// </summary>
    /// <param name="nome">Nome informado.</param>
    /// <returns><c>true</c> se o nome é aceitável.</returns>
    private static bool NomeValido(string? nome)
    {
        var aparado = nome?.Trim();
        return aparado is { Length: >= Usuario.NomeTamanhoMinimo and <= Usuario.NomeTamanhoMaximo }
            && aparado.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2;
    }
}
