namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Traduz os erros das APIs em mensagens para o usuário, no texto do manual. Mensagens de validação nunca repetem o valor digitado.
/// </summary>
public static class TradutorDeErros
{
    /// <summary>
    /// Mensagens por código estável de erro de negócio.
    /// </summary>
    private static readonly Dictionary<string, string> _porCodigo = new()
    {
        ["campanha.data_fim_passada"] = "A data de término não pode estar no passado.",
        ["campanha.periodo_invalido"] = "A data de término não pode ser anterior à data de início.",
        ["campanha.titulo_invalido"] = "O título deve ter de 3 a 120 caracteres.",
        ["campanha.descricao_invalida"] = "A descrição deve ter até 2000 caracteres.",
        ["campanha.versao_obrigatoria"] = "Recarregue a página antes de salvar a campanha.",
        ["campanha.meta_invalida"] = "A meta deve ser maior que zero.",
        ["campanha.nao_editavel"] = "A campanha não pode ser editada.",
        ["campanha.transicao_invalida"] = "A mudança de status não é permitida para esta campanha.",
        ["campanha.conflito_de_versao"] = "A campanha foi alterada por outra pessoa. Recarregue a página e refaça a alteração.",
        ["campanha.nao_encontrada"] = "Campanha não encontrada.",
        ["doacao.campanha_nao_ativa"] = "A campanha não está aceitando doações.",
        ["doacao.valor_invalido"] = "O valor da doação é inválido.",
        ["doacao.chave_idempotencia_conflitante"] = "Esta tentativa de doação já foi usada com outro valor. Refaça a doação.",
        ["servico.indisponivel"] = "O serviço está indisponível no momento. Tente novamente em instantes.",
    };

    /// <summary>
    /// Obtém as mensagens de um erro: as de validação por campo quando há; senão a do código conhecido; senão a da API com o
    /// código de rastreio, quando ele veio.
    /// </summary>
    /// <param name="erro">Erro devolvido pela API.</param>
    /// <returns>As mensagens, ao menos uma, sem repetição.</returns>
    public static IReadOnlyList<string> Mensagens(ErroApi erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        if (erro.Campos.Count > 0)
        {
            return [.. erro.Campos.SelectMany(c => c.Value).Distinct()];
        }

        if (erro.Codigo is not null && _porCodigo.TryGetValue(erro.Codigo, out var mensagem))
        {
            return [mensagem];
        }

        if (erro.Status == 403)
        {
            return ["Você não tem permissão para esta ação."];
        }

        return [erro.Mensagem + (erro.TraceId is null ? string.Empty : $" Código de rastreio: {erro.TraceId}.")];
    }
}
