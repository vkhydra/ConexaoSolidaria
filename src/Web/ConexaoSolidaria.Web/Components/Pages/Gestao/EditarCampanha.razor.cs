using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages.Gestao;

/// <summary>
/// Detalhe, edição e mudança de status (concluir ou cancelar, com confirmação) de uma campanha. A edição envia a versão lida
/// em <c>If-Match</c>: se outra pessoa salvou antes, a API responde 409 e a tela pede para recarregar.
/// </summary>
public partial class EditarCampanha
{
    /// <summary>
    /// Obtém ou define o identificador da campanha, vindo da rota.
    /// </summary>
    [Parameter]
    public Guid Id { get; set; }

    /// <summary>
    /// Obtém ou define o cliente da Campanhas.Api.
    /// </summary>
    [Inject]
    public IClienteCampanhas Campanhas { get; set; } = default!;

    /// <summary>
    /// Obtém a campanha carregada; nula enquanto carrega ou quando não existe.
    /// </summary>
    public CampanhaGestao? Atual { get; private set; }

    /// <summary>
    /// Obtém os dados do formulário de edição.
    /// </summary>
    public ModeloDeCampanha Modelo { get; private set; } = new();

    /// <summary>
    /// Obtém as mensagens de erro da última tentativa.
    /// </summary>
    public IReadOnlyList<string> Mensagens { get; private set; } = [];

    /// <summary>
    /// Obtém a mensagem de erro de carga, mostrada no lugar da campanha.
    /// </summary>
    public string? MensagemDeCarga { get; private set; }

    /// <summary>
    /// Obtém o aviso de sucesso da última ação.
    /// </summary>
    public string? Aviso { get; private set; }

    /// <summary>
    /// Obtém o status que aguarda confirmação (<c>Concluida</c> ou <c>Cancelada</c>); nulo quando não há confirmação aberta.
    /// </summary>
    public string? Confirmando { get; private set; }

    /// <summary>
    /// Obtém um valor que indica se uma ação está em andamento.
    /// </summary>
    public bool Enviando { get; private set; }

    /// <summary>
    /// Versão (ETag) lida com a campanha, enviada em <c>If-Match</c> ao salvar.
    /// </summary>
    private string _versao = string.Empty;

    /// <inheritdoc/>
    protected override Task OnInitializedAsync()
    {
        return CarregarAsync();
    }

    /// <summary>
    /// Salva as alterações enviando a versão lida; em conflito, avisa para recarregar sem perder o que a outra pessoa gravou.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task SalvarAsync()
    {
        Enviando = true;
        try
        {
            var resposta = await Campanhas.AtualizarAsync(Id, _versao, Modelo.ParaRequisicao());
            if (resposta.Sucesso)
            {
                Mensagens = [];
                await CarregarAsync();
                Aviso = "Alterações salvas.";
                return;
            }

            Mensagens = TradutorDeErros.Mensagens(resposta.Erro!);
        }
        finally
        {
            Enviando = false;
        }
    }

    /// <summary>
    /// Abre a confirmação para concluir ou cancelar.
    /// </summary>
    /// <param name="status">Status de destino (<c>Concluida</c> ou <c>Cancelada</c>).</param>
    public void Confirmar(string status)
    {
        Confirmando = status;
    }

    /// <summary>
    /// Fecha a confirmação sem alterar a campanha.
    /// </summary>
    public void Desistir()
    {
        Confirmando = null;
    }

    /// <summary>
    /// Aplica a mudança de status confirmada e recarrega a campanha.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task AlterarStatusAsync()
    {
        if (Confirmando is null)
        {
            return;
        }

        Enviando = true;
        try
        {
            var status = Confirmando;
            var resposta = await Campanhas.AlterarStatusAsync(Id, status);
            Confirmando = null;
            if (resposta.Sucesso)
            {
                Mensagens = [];
                await CarregarAsync();
                Aviso = status == "Concluida" ? "Campanha concluída." : "Campanha cancelada.";
                return;
            }

            Mensagens = TradutorDeErros.Mensagens(resposta.Erro!);
        }
        finally
        {
            Enviando = false;
        }
    }

    /// <summary>
    /// Lê a campanha e a versão atuais da API.
    /// </summary>
    /// <returns>Tarefa que termina com a campanha carregada.</returns>
    private async Task CarregarAsync()
    {
        Aviso = null;
        var resposta = await Campanhas.ObterAsync(Id);
        if (!resposta.Sucesso)
        {
            Atual = null;
            MensagemDeCarga = string.Join(' ', TradutorDeErros.Mensagens(resposta.Erro!));
            return;
        }

        MensagemDeCarga = null;
        Atual = resposta.Valor.Campanha;
        _versao = resposta.Valor.Versao;
        Modelo = ModeloDeCampanha.De(Atual);
    }
}
