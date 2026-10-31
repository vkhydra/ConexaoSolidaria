namespace Identity.Application.Doadores;

/// <summary>
/// Doador cadastrado. Nunca contém a senha nem o CPF completo.
/// </summary>
/// <param name="Id">Identificador do doador.</param>
/// <param name="NomeCompleto">Nome completo.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="CpfMascarado">CPF com os dígitos das pontas ocultos.</param>
/// <param name="CriadoEm">Data e hora do cadastro.</param>
public sealed record CadastrarDoadorResponse(Guid Id, string NomeCompleto, string Email, string CpfMascarado, DateTimeOffset CriadoEm);
