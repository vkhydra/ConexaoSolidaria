using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Busca pública de campanhas, tolerante a erros de digitação. O texto e a página vêm da consulta da URL (formulário GET),
/// então o resultado pode ser compartilhado por link. O trecho destacado é desenhado como texto, com só a marcação do termo.
/// </summary>
public partial class Busca
{
    /// <summary>
    /// Menor tamanho do texto aceito pela API.
    /// </summary>
    private const int _tamanhoMinimo = 2;

    /// <summary>
    /// Obtém ou define o cliente da Campanhas.Api.
    /// </summary>
    [Inject]
    public IClienteCampanhas Campanhas { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o texto procurado (parâmetro <c>q</c>).
    /// </summary>
    [SupplyParameterFromQuery(Name = "q")]
    public string? Q { get; set; }

    /// <summary>
    /// Obtém ou define o número da página (parâmetro <c>page</c>); 1 quando omitido.
    /// </summary>
    [SupplyParameterFromQuery(Name = "page")]
    public int? Pagina { get; set; }

    /// <summary>
    /// Obtém a página de resultados; nula antes da busca ou quando houve erro.
    /// </summary>
    public BuscaResposta? Resultado { get; private set; }

    /// <summary>
    /// Obtém a mensagem de erro mostrada no lugar dos resultados; nula quando a busca deu certo.
    /// </summary>
    public string? Mensagem { get; private set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        Resultado = null;
        Mensagem = null;
        if (Q is null)
        {
            return;
        }

        if (Q.Trim().Length < _tamanhoMinimo)
        {
            Mensagem = "Digite ao menos 2 letras para buscar.";
            return;
        }

        var resposta = await Campanhas.BuscarAsync(Q.Trim(), Math.Max(Pagina ?? 1, 1));
        if (resposta.Sucesso)
        {
            Resultado = resposta.Valor;
            return;
        }

        Mensagem = resposta.Erro!.Status switch
        {
            400 => "Digite entre 2 e 100 caracteres para buscar.",
            503 => "Busca temporariamente indisponível. Use o painel de transparência ou tente mais tarde.",
            _ => resposta.Erro.Mensagem,
        };
    }
}
