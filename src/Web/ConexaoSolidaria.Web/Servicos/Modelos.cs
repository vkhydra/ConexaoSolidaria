namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Resposta do login: o JWT e o papel do usuário.
/// </summary>
/// <param name="AccessToken">JWT emitido pela Identity.</param>
/// <param name="TokenType">Tipo do token (<c>Bearer</c>).</param>
/// <param name="ExpiresIn">Validade do token, em segundos.</param>
/// <param name="Role">Papel do usuário (<c>Doador</c> ou <c>GestorONG</c>).</param>
public sealed record LoginResposta(string AccessToken, string TokenType, int ExpiresIn, string Role);

/// <summary>
/// Dados do cadastro de um doador.
/// </summary>
/// <param name="NomeCompleto">Nome e sobrenome.</param>
/// <param name="Email">E-mail.</param>
/// <param name="Cpf">CPF, com ou sem pontuação.</param>
/// <param name="Senha">Senha em texto, enviada só no cadastro.</param>
/// <param name="AceitePoliticaPrivacidade">Se a pessoa aceitou a política de privacidade.</param>
public sealed record CadastroDoadorRequest(string NomeCompleto, string Email, string Cpf, string Senha, bool AceitePoliticaPrivacidade);

/// <summary>
/// Doador recém-cadastrado.
/// </summary>
/// <param name="Id">Identificador do doador.</param>
/// <param name="NomeCompleto">Nome.</param>
/// <param name="Email">E-mail.</param>
/// <param name="CpfMascarado">CPF mascarado.</param>
/// <param name="CriadoEm">Instante do cadastro.</param>
public sealed record DoadorResposta(Guid Id, string NomeCompleto, string Email, string CpfMascarado, DateTimeOffset CriadoEm);

/// <summary>
/// Consentimento registrado no cadastro.
/// </summary>
/// <param name="Versao">Versão da política aceita.</param>
/// <param name="AceitoEm">Instante do aceite.</param>
public sealed record ConsentimentoResposta(string Versao, DateTimeOffset AceitoEm);

/// <summary>
/// Dados do titular, para a página Meus dados.
/// </summary>
/// <param name="Id">Identificador do doador.</param>
/// <param name="NomeCompleto">Nome.</param>
/// <param name="Email">E-mail.</param>
/// <param name="CpfMascarado">CPF mascarado.</param>
/// <param name="CriadoEm">Instante do cadastro.</param>
/// <param name="Consentimento">Consentimento registrado.</param>
public sealed record MeusDadosResposta(Guid Id, string NomeCompleto, string Email, string CpfMascarado, DateTimeOffset CriadoEm, ConsentimentoResposta Consentimento);

/// <summary>
/// Campanha ativa no painel de transparência.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título.</param>
/// <param name="MetaFinanceira">Meta, em reais.</param>
/// <param name="TotalArrecadado">Total arrecadado, em reais.</param>
/// <param name="PercentualAtingido">Percentual da meta atingido, com duas casas.</param>
/// <param name="DataFim">Último dia da campanha.</param>
public sealed record PainelItem(Guid Id, string Titulo, decimal MetaFinanceira, decimal TotalArrecadado, decimal PercentualAtingido, DateOnly DataFim);

/// <summary>
/// Campanha encontrada pela busca.
/// </summary>
/// <param name="Id">Identificador da campanha.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Trecho">Trecho da descrição com os termos entre <c>&lt;em&gt;</c>; nulo se a descrição não casou.</param>
/// <param name="Relevancia">Pontuação da busca.</param>
/// <param name="DataFim">Último dia da campanha.</param>
public sealed record BuscaItem(Guid Id, string Titulo, string? Trecho, double Relevancia, DateOnly DataFim);

/// <summary>
/// Página de resultados da busca.
/// </summary>
/// <param name="Items">Campanhas da página.</param>
/// <param name="Page">Número da página.</param>
/// <param name="PageSize">Itens por página.</param>
/// <param name="TotalItems">Total de resultados.</param>
/// <param name="TotalPages">Total de páginas.</param>
public sealed record BuscaResposta(IReadOnlyList<BuscaItem> Items, int Page, int PageSize, int TotalItems, int TotalPages);

/// <summary>
/// Campanha na gestão.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="DataInicio">Data de início.</param>
/// <param name="DataFim">Data de término.</param>
/// <param name="MetaFinanceira">Meta, em reais.</param>
/// <param name="TotalArrecadado">Total arrecadado, em reais.</param>
/// <param name="PercentualAtingido">Percentual da meta atingido.</param>
/// <param name="Status">Situação (<c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>).</param>
public sealed record CampanhaGestao(Guid Id, string Titulo, string Descricao, DateOnly DataInicio, DateOnly DataFim, decimal MetaFinanceira, decimal TotalArrecadado, decimal PercentualAtingido, string Status);

/// <summary>
/// Campanha com a versão (ETag) a enviar em <c>If-Match</c> na edição.
/// </summary>
/// <param name="Campanha">Campanha lida.</param>
/// <param name="Versao">Versão (ETag) lida com ela, entre aspas.</param>
public sealed record CampanhaComVersao(CampanhaGestao Campanha, string Versao);

/// <summary>
/// Página de campanhas da gestão.
/// </summary>
/// <param name="Items">Campanhas da página.</param>
/// <param name="Page">Número da página.</param>
/// <param name="PageSize">Itens por página.</param>
/// <param name="TotalItems">Total de campanhas.</param>
/// <param name="TotalPages">Total de páginas.</param>
public sealed record PaginaDeCampanhas(IReadOnlyList<CampanhaGestao> Items, int Page, int PageSize, int TotalItems, int TotalPages);

/// <summary>
/// Dados do formulário de criação ou edição de campanha.
/// </summary>
/// <param name="Titulo">Título.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="DataInicio">Data de início.</param>
/// <param name="DataFim">Data de término.</param>
/// <param name="MetaFinanceira">Meta, em reais.</param>
public sealed record CampanhaRequest(string Titulo, string Descricao, DateOnly DataInicio, DateOnly DataFim, decimal MetaFinanceira);

/// <summary>
/// Doação aceita pela API (202).
/// </summary>
/// <param name="DoacaoId">Identificador da doação.</param>
/// <param name="Status">Situação (<c>Recebida</c>).</param>
/// <param name="Mensagem">Mensagem da API.</param>
public sealed record DoacaoAceita(Guid DoacaoId, string Status, string Mensagem);
