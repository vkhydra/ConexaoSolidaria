using System.ComponentModel.DataAnnotations;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Página de login. É renderizada no servidor (SSR estático) para que o POST do formulário possa gravar o cookie de
/// autenticação; o JWT recebido da Identity fica dentro desse cookie e nunca vai ao navegador de outra forma.
/// </summary>
public partial class Entrar
{
    /// <summary>
    /// Obtém ou define o cliente da Identity.Api.
    /// </summary>
    [Inject]
    public IClienteIdentity Identity { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o serviço que grava o cookie da sessão.
    /// </summary>
    [Inject]
    public IServicoDeSessao Sessao { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o gerenciador de navegação.
    /// </summary>
    [Inject]
    public NavigationManager Navegacao { get; set; } = default!;

    /// <summary>
    /// Obtém ou define o contexto HTTP da requisição, necessário para gravar o cookie.
    /// </summary>
    [CascadingParameter]
    public HttpContext? Contexto { get; set; }

    /// <summary>
    /// Obtém ou define o endereço local para onde voltar depois do login (parâmetro <c>retorno</c>).
    /// </summary>
    [SupplyParameterFromQuery(Name = "retorno")]
    public string? Retorno { get; set; }

    /// <summary>
    /// Obtém ou define se a pessoa acabou de criar a conta (parâmetro <c>cadastrado</c>), para mostrar o aviso.
    /// </summary>
    [SupplyParameterFromQuery(Name = "cadastrado")]
    public bool Cadastrado { get; set; }

    /// <summary>
    /// Obtém ou define os dados do formulário.
    /// </summary>
    [SupplyParameterFromForm]
    public ModeloDeLogin? Modelo { get; set; }

    /// <summary>
    /// Obtém a mensagem de erro mostrada acima do formulário; nula quando não há erro.
    /// </summary>
    public string? Mensagem { get; private set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        Modelo ??= new ModeloDeLogin();
    }

    /// <summary>
    /// Autentica na Identity; se der certo, grava a sessão e vai para o início (ou para o endereço de retorno local). Se falhar,
    /// mostra uma mensagem que nunca diz qual dos dois dados está errado.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task EnviarAsync()
    {
        var resposta = await Identity.LoginAsync(Modelo!.Email, Modelo.Senha);
        if (!resposta.Sucesso)
        {
            Mensagem = MensagemDe(resposta.Erro!);
            return;
        }

        await Sessao.EntrarAsync(Contexto!, resposta.Valor, Modelo.Email);
        Navegacao.NavigateTo(DestinoSeguro(Retorno), forceLoad: true);
    }

    /// <summary>
    /// Aceita só endereços locais (começados por uma barra e sem barra dupla), para o login não virar redirecionamento aberto.
    /// </summary>
    /// <param name="retorno">Endereço pedido.</param>
    /// <returns>O endereço, ou <c>/</c> se não for local.</returns>
    public static string DestinoSeguro(string? retorno)
    {
        return !string.IsNullOrEmpty(retorno) && retorno[0] == '/' && !retorno.StartsWith("//", StringComparison.Ordinal) && !retorno.StartsWith("/\\", StringComparison.Ordinal)
            ? retorno
            : "/";
    }

    /// <summary>
    /// Escolhe a mensagem do usuário para o erro da API.
    /// </summary>
    /// <param name="erro">Erro devolvido pela Identity.</param>
    /// <returns>A mensagem em português.</returns>
    private static string MensagemDe(ErroApi erro)
    {
        return erro.Status switch
        {
            401 => "E-mail ou senha inválidos.",
            429 => "Muitas tentativas. Tente novamente em alguns minutos.",
            400 => "Informe um e-mail e uma senha válidos.",
            _ => erro.Mensagem + (erro.TraceId is null ? string.Empty : $" Código de rastreio: {erro.TraceId}."),
        };
    }

    /// <summary>
    /// Dados do formulário de login.
    /// </summary>
    public sealed class ModeloDeLogin
    {
        /// <summary>
        /// Obtém ou define o e-mail.
        /// </summary>
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define a senha.
        /// </summary>
        [Required(ErrorMessage = "Informe a senha.")]
        public string Senha { get; set; } = string.Empty;
    }
}
