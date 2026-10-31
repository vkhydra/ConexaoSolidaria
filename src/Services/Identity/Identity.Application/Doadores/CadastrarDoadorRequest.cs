namespace Identity.Application.Doadores;

/// <summary>
/// Dados do cadastro de doador. Todos são dados pessoais ou segredos: nunca vão para log.
/// </summary>
/// <param name="NomeCompleto">Nome completo.</param>
/// <param name="Email">E-mail, em qualquer caixa.</param>
/// <param name="Cpf">CPF com ou sem máscara.</param>
/// <param name="Senha">Senha em texto.</param>
/// <param name="AceitePoliticaPrivacidade">Se o visitante aceitou a política de privacidade.</param>
public sealed record CadastrarDoadorRequest(
    string? NomeCompleto,
    string? Email,
    string? Cpf,
    string? Senha,
    bool AceitePoliticaPrivacidade);
