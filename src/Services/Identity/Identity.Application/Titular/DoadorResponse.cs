namespace Identity.Application.Titular;

/// <summary>
/// Dados do doador devolvidos a ele mesmo (direito de acesso da LGPD). O CPF vem sempre mascarado.
/// </summary>
/// <param name="Id">Identificador do doador.</param>
/// <param name="NomeCompleto">Nome completo.</param>
/// <param name="Email">E-mail.</param>
/// <param name="CpfMascarado">CPF com os dígitos das pontas ocultos.</param>
/// <param name="CriadoEm">Data e hora do cadastro.</param>
/// <param name="Consentimento">Aceite da política de privacidade.</param>
public sealed record DoadorResponse(
    Guid Id,
    string NomeCompleto,
    string Email,
    string CpfMascarado,
    DateTimeOffset CriadoEm,
    ConsentimentoResponse Consentimento);

/// <summary>
/// Versão e data do aceite da política de privacidade.
/// </summary>
/// <param name="Versao">Versão da política aceita.</param>
/// <param name="AceitoEm">Data e hora do aceite.</param>
public sealed record ConsentimentoResponse(string Versao, DateTimeOffset AceitoEm);
