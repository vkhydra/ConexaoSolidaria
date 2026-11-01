using System.ComponentModel.DataAnnotations;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Fluxo de doação. Cada tentativa tem uma <c>Idempotency-Key</c> própria, reaproveitada se a mesma tentativa for reenviada
/// (por exemplo, depois de uma falha de rede ou de um clique duplo); mudar o valor começa uma tentativa nova. O botão fica
/// desabilitado durante o envio.
/// </summary>
public partial class Doar
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
    /// Obtém o título da campanha, quando ela aparece no painel; nulo se não foi encontrada.
    /// </summary>
    public string? Titulo { get; private set; }

    /// <summary>
    /// Obtém os dados do formulário.
    /// </summary>
    public ModeloDeDoacao Modelo { get; }

    /// <summary>
    /// Obtém as mensagens de erro da última tentativa.
    /// </summary>
    public IReadOnlyList<string> Mensagens { get; private set; } = [];

    /// <summary>
    /// Obtém a doação aceita; nula até a API responder 202.
    /// </summary>
    public DoacaoAceita? Recibo { get; private set; }

    /// <summary>
    /// Obtém um valor que indica se o envio está em andamento.
    /// </summary>
    public bool Enviando { get; private set; }

    /// <summary>
    /// Obtém a chave de idempotência da tentativa atual.
    /// </summary>
    public string ChaveDaTentativa { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Inicializa a página ligando a mudança de valor ao começo de uma tentativa nova.
    /// </summary>
    public Doar()
    {
        Modelo = new ModeloDeDoacao();
        Modelo.ValorMudou += NovaTentativa;
    }

    /// <summary>
    /// Começa uma tentativa nova, com chave nova, porque o valor mudou.
    /// </summary>
    public void NovaTentativa()
    {
        ChaveDaTentativa = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Envia a doação com a chave da tentativa. Cliques repetidos durante o envio são ignorados.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task DoarAsync()
    {
        if (Enviando)
        {
            return;
        }

        Enviando = true;
        try
        {
            var resposta = await Campanhas.DoarAsync(Id, decimal.Round(Modelo.Valor, 2), ChaveDaTentativa);
            if (resposta.Sucesso)
            {
                Recibo = resposta.Valor;
                Mensagens = [];
                return;
            }

            Mensagens = TradutorDeErros.Mensagens(resposta.Erro!);
        }
        finally
        {
            Enviando = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var painel = await Campanhas.ObterPainelAsync();
        Titulo = painel.Sucesso ? painel.Valor.FirstOrDefault(c => c.Id == Id)?.Titulo : null;
    }

    /// <summary>
    /// Dados do formulário de doação.
    /// </summary>
    public sealed class ModeloDeDoacao
    {
        /// <summary>
        /// Valor da doação, em reais.
        /// </summary>
        private decimal _valor;

        /// <summary>
        /// Ocorre quando o valor muda, o que começa uma tentativa de doação nova.
        /// </summary>
        public event Action? ValorMudou;

        /// <summary>
        /// Obtém ou define o valor da doação, em reais.
        /// </summary>
        [Range(0.01, 1000000.00, ErrorMessage = "O valor deve ser maior que zero e ter no máximo R$ 1.000.000,00.")]
        public decimal Valor
        {
            get => _valor;
            set
            {
                if (_valor != value)
                {
                    _valor = value;
                    ValorMudou?.Invoke();
                }
            }
        }
    }
}
