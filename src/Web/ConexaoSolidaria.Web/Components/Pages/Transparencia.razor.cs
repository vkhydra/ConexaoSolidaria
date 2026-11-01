using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Painel público de transparência: campanhas ativas, meta, total arrecadado e progresso. Renderizado no servidor (SSR
/// estático), sem circuito; a leitura vem do cache da API, então o total pode atrasar alguns segundos.
/// </summary>
public partial class Transparencia
{
    /// <summary>
    /// Obtém ou define o cliente da Campanhas.Api.
    /// </summary>
    [Inject]
    public IClienteCampanhas Campanhas { get; set; } = default!;

    /// <summary>
    /// Obtém as campanhas do painel; nulo enquanto não carregou ou quando houve erro.
    /// </summary>
    public IReadOnlyList<PainelItem>? Itens { get; private set; }

    /// <summary>
    /// Obtém a mensagem de erro mostrada no lugar da lista; nula quando a leitura deu certo.
    /// </summary>
    public string? Mensagem { get; private set; }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var resposta = await Campanhas.ObterPainelAsync();
        if (resposta.Sucesso)
        {
            Itens = resposta.Valor;
            return;
        }

        Mensagem = "O painel está temporariamente indisponível. Tente novamente em instantes.";
    }
}
