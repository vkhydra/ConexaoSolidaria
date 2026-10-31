using Campanhas.Domain.Comum;

namespace Campanhas.Application.Campanhas;

/// <summary>
/// Erros de negócio dos casos de uso de campanha que não vêm do agregado.
/// </summary>
public static class ErrosDeCampanha
{
    /// <summary>
    /// Código do erro de campanha inexistente.
    /// </summary>
    public const string CodigoNaoEncontrada = "campanha.nao_encontrada";

    /// <summary>
    /// Código do erro de edição sem a versão esperada.
    /// </summary>
    public const string CodigoVersaoObrigatoria = "campanha.versao_obrigatoria";

    /// <summary>
    /// Código do erro de edição concorrente: a versão enviada não é mais a atual.
    /// </summary>
    public const string CodigoConflitoDeVersao = "campanha.conflito_de_versao";

    /// <summary>
    /// Obtém o erro de campanha inexistente.
    /// </summary>
    public static Erro NaoEncontrada { get; } = new(CodigoNaoEncontrada, "Campanha não encontrada.");

    /// <summary>
    /// Obtém o erro de versão não informada.
    /// </summary>
    public static Erro VersaoObrigatoria { get; } = new(CodigoVersaoObrigatoria, "Informe a versão da campanha no cabeçalho If-Match.");

    /// <summary>
    /// Obtém o erro de conflito de versão.
    /// </summary>
    public static Erro ConflitoDeVersao { get; } = new(CodigoConflitoDeVersao, "A campanha foi alterada por outra pessoa. Recarregue e tente de novo.");
}
