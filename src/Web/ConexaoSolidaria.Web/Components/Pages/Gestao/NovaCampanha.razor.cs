using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages.Gestao;

/// <summary>
/// Criação de campanha pelo gestor. A campanha nasce Ativa, com arrecadado zerado; a API aplica as regras de negócio.
/// </summary>
public partial class NovaCampanha
{
    /// <summary>
    /// Obtém ou define o cliente da Campanhas.Api.
    /// </summary>
    [Inject]
    public IClienteCampanhas Campanhas { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o gerenciador de navegação.
    /// </summary>
    [Inject]
    public NavigationManager Navegacao { get; set; } = default!;

    /// <summary>
    /// Obtém os dados do formulário.
    /// </summary>
    public ModeloDeCampanha Modelo { get; } = new();

    /// <summary>
    /// Obtém as mensagens de erro da última tentativa.
    /// </summary>
    public IReadOnlyList<string> Mensagens { get; private set; } = [];

    /// <summary>
    /// Obtém um valor que indica se o envio está em andamento.
    /// </summary>
    public bool Enviando { get; private set; }

    /// <summary>
    /// Cria a campanha; se der certo, abre a campanha criada. Se falhar, mostra as mensagens e mantém o formulário preenchido.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task SalvarAsync()
    {
        Enviando = true;
        try
        {
            var resposta = await Campanhas.CriarAsync(Modelo.ParaRequisicao());
            if (resposta.Sucesso)
            {
                Navegacao.NavigateTo($"/gestao/campanhas/{resposta.Valor.Id}");
                return;
            }

            Mensagens = TradutorDeErros.Mensagens(resposta.Erro!);
        }
        finally
        {
            Enviando = false;
        }
    }
}
