using Microsoft.AspNetCore.Http;

namespace ConexaoSolidaria.ServiceDefaults.Erros;

/// <summary>
/// Códigos e títulos padrão das respostas de erro HTTP (RFC 9457, <c>application/problem+json</c>).
/// O <c>code</c> é estável: clientes e testes dependem dele, não do texto.
/// </summary>
public static class CatalogoDeErros
{
    /// <summary>
    /// Prefixo do campo <c>type</c> das respostas de erro.
    /// </summary>
    public const string PrefixoTipo = "https://conexao.local/erros/";

    /// <summary>
    /// Código do erro inesperado (HTTP 500).
    /// </summary>
    public const string ErroInesperado = "erro.inesperado";

    /// <summary>
    /// Código da requisição mal formada (HTTP 400).
    /// </summary>
    public const string Validacao = "validacao";

    /// <summary>
    /// Código da falta de autenticação (HTTP 401).
    /// </summary>
    public const string NaoAutenticado = "auth.nao_autenticado";

    /// <summary>
    /// Código da falta de permissão (HTTP 403).
    /// </summary>
    public const string SemPermissao = "auth.sem_permissao";

    /// <summary>
    /// Código do recurso inexistente (HTTP 404).
    /// </summary>
    public const string NaoEncontrado = "recurso.nao_encontrado";

    /// <summary>
    /// Código genérico para os demais status HTTP.
    /// </summary>
    public const string ErroHttp = "erro.http";

    /// <summary>
    /// Obtém o código padrão de um status HTTP.
    /// </summary>
    /// <param name="status">Status HTTP da resposta.</param>
    /// <returns>Código estável do erro.</returns>
    public static string CodigoPara(int status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest => Validacao,
            StatusCodes.Status401Unauthorized => NaoAutenticado,
            StatusCodes.Status403Forbidden => SemPermissao,
            StatusCodes.Status404NotFound => NaoEncontrado,
            StatusCodes.Status500InternalServerError => ErroInesperado,
            _ => ErroHttp,
        };
    }

    /// <summary>
    /// Obtém o título padrão, em português, de um status HTTP.
    /// </summary>
    /// <param name="status">Status HTTP da resposta.</param>
    /// <returns>Título legível.</returns>
    public static string TituloPara(int status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest => "Dados inválidos",
            StatusCodes.Status401Unauthorized => "Autenticação necessária",
            StatusCodes.Status403Forbidden => "Acesso negado",
            StatusCodes.Status404NotFound => "Recurso não encontrado",
            StatusCodes.Status500InternalServerError => "Erro inesperado",
            _ => "Falha na requisição",
        };
    }
}
