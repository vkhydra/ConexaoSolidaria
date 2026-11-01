using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages.Gestao;

/// <summary>
/// Lista de campanhas da gestão, com filtro por status e paginação. Exclusiva do papel <c>GestorONG</c>; a API valida de novo.
/// </summary>
public partial class ListaDeCampanhas
{
    /// <summary>
    /// Obtém ou define o cliente da Campanhas.Api.
    /// </summary>
    [Inject]
    public IClienteCampanhas Campanhas { get; set; } = default!;

    /// <summary>
    /// Obtém o status filtrado; nulo para todas as campanhas.
    /// </summary>
    public string? Filtro { get; private set; }

    /// <summary>
    /// Obtém a página atual; nula enquanto carrega ou quando houve erro.
    /// </summary>
    public PaginaDeCampanhas? Pagina { get; private set; }

    /// <summary>
    /// Obtém a mensagem de erro mostrada no lugar da lista.
    /// </summary>
    public string? Mensagem { get; private set; }

    /// <inheritdoc/>
    protected override Task OnInitializedAsync()
    {
        return IrParaAsync(1);
    }

    /// <summary>
    /// Troca o filtro de status e volta à primeira página.
    /// </summary>
    /// <param name="evento">Evento de mudança do seletor.</param>
    /// <returns>Tarefa que termina com a lista recarregada.</returns>
    public Task MudarFiltroAsync(ChangeEventArgs evento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var valor = evento.Value?.ToString();
        Filtro = string.IsNullOrEmpty(valor) ? null : valor;
        return IrParaAsync(1);
    }

    /// <summary>
    /// Carrega uma página da lista com o filtro atual.
    /// </summary>
    /// <param name="numero">Número da página.</param>
    /// <returns>Tarefa que termina com a lista carregada.</returns>
    public async Task IrParaAsync(int numero)
    {
        var resposta = await Campanhas.ListarAsync(Filtro, numero);
        if (resposta.Sucesso)
        {
            Pagina = resposta.Valor;
            Mensagem = null;
            return;
        }

        Pagina = null;
        Mensagem = string.Join(' ', TradutorDeErros.Mensagens(resposta.Erro!));
    }
}
