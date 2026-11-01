using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Direitos do titular (LGPD): mostra os dados do cadastro, com CPF mascarado, e permite excluí-los (anonimizar) depois de
/// uma confirmação. Depois do 204, a sessão é encerrada, porque o login deixa de existir.
/// </summary>
public partial class MeusDados
{
    /// <summary>
    /// Endereço que encerra a sessão depois da exclusão e volta ao início com o aviso.
    /// </summary>
    public const string EnderecoDeSaida = "/sessao-encerrada";

    /// <summary>
    /// Obtém ou define o cliente da Identity.Api.
    /// </summary>
    [Inject]
    public IClienteIdentity Identity { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o gerenciador de navegação.
    /// </summary>
    [Inject]
    public NavigationManager Navegacao { get; set; } = default!;

    /// <summary>
    /// Obtém os dados do titular; nulos enquanto carrega ou quando houve erro.
    /// </summary>
    public MeusDadosResposta? Dados { get; private set; }

    /// <summary>
    /// Obtém a mensagem de erro mostrada na página.
    /// </summary>
    public string? Mensagem { get; private set; }

    /// <summary>
    /// Obtém um valor que indica se a confirmação da exclusão está aberta.
    /// </summary>
    public bool Confirmando { get; private set; }

    /// <summary>
    /// Obtém um valor que indica se a exclusão está em andamento.
    /// </summary>
    public bool Excluindo { get; private set; }

    /// <summary>
    /// Abre a confirmação da exclusão.
    /// </summary>
    public void PedirConfirmacao()
    {
        Confirmando = true;
    }

    /// <summary>
    /// Fecha a confirmação sem excluir nada.
    /// </summary>
    public void Desistir()
    {
        Confirmando = false;
    }

    /// <summary>
    /// Exclui os dados do titular; se der certo (204), encerra a sessão. Se falhar, mostra a mensagem e mantém a sessão.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task ExcluirAsync()
    {
        Excluindo = true;
        try
        {
            var resposta = await Identity.ExcluirMeusDadosAsync();
            if (resposta.Sucesso)
            {
                Navegacao.NavigateTo(EnderecoDeSaida, forceLoad: true);
                return;
            }

            Confirmando = false;
            Mensagem = string.Join(' ', TradutorDeErros.Mensagens(resposta.Erro!));
        }
        finally
        {
            Excluindo = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var resposta = await Identity.ObterMeusDadosAsync();
        if (resposta.Sucesso)
        {
            Dados = resposta.Valor;
            return;
        }

        Mensagem = resposta.Erro!.Status == 401
            ? "Sua sessão expirou. Entre de novo."
            : string.Join(' ', TradutorDeErros.Mensagens(resposta.Erro));
    }
}
