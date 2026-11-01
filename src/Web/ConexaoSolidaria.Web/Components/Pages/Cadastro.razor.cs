using System.ComponentModel.DataAnnotations;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace ConexaoSolidaria.Web.Components.Pages;

/// <summary>
/// Página de cadastro de doador. Renderizada no servidor; o aceite da política de privacidade é obrigatório e a senha só
/// trafega neste envio, direto do servidor da interface para a Identity.
/// </summary>
public partial class Cadastro
{
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
    /// Obtém ou define os dados do formulário.
    /// </summary>
    [SupplyParameterFromForm]
    public ModeloDeCadastro? Modelo { get; set; }

    /// <summary>
    /// Obtém as mensagens de erro mostradas acima do formulário; vazia quando não há erro.
    /// </summary>
    public List<string> Mensagens { get; } = [];

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        Modelo ??= new ModeloDeCadastro();
    }

    /// <summary>
    /// Cadastra o doador; se der certo, abre a tela de login com o aviso de conta criada. Se falhar, mostra as mensagens da
    /// API (e-mail ou CPF em uso, campos inválidos), sem repetir o valor digitado.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta tratada.</returns>
    public async Task EnviarAsync()
    {
        Mensagens.Clear();
        var m = Modelo!;
        var resposta = await Identity.CadastrarAsync(new CadastroDoadorRequest(m.NomeCompleto.Trim(), m.Email.Trim(), m.Cpf.Trim(), m.Senha, m.AceitePolitica));
        if (resposta.Sucesso)
        {
            Navegacao.NavigateTo("/entrar?cadastrado=true");
            return;
        }

        Mensagens.AddRange(MensagensDe(resposta.Erro!));
    }

    /// <summary>
    /// Traduz o erro da API em mensagens para o usuário.
    /// </summary>
    /// <param name="erro">Erro devolvido pela Identity.</param>
    /// <returns>As mensagens, ao menos uma.</returns>
    private static IEnumerable<string> MensagensDe(ErroApi erro)
    {
        switch (erro.Codigo)
        {
            case "doador.email_em_uso":
                return ["Este e-mail já está em uso."];
            case "doador.cpf_em_uso":
                return ["Este CPF já está cadastrado."];
        }

        if (erro.Status == 400 && erro.Campos.Count > 0)
        {
            return erro.Campos.SelectMany(c => c.Value).Distinct();
        }

        return [erro.Status == 429 ? "Muitas tentativas. Tente novamente em alguns minutos." : erro.Mensagem + (erro.TraceId is null ? string.Empty : $" Código de rastreio: {erro.TraceId}.")];
    }

    /// <summary>
    /// Dados do formulário de cadastro.
    /// </summary>
    public sealed class ModeloDeCadastro
    {
        /// <summary>
        /// Obtém ou define o nome completo.
        /// </summary>
        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "O nome deve ter de 3 a 150 caracteres.")]
        public string NomeCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define o e-mail.
        /// </summary>
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define o CPF.
        /// </summary>
        [Required(ErrorMessage = "Informe o CPF.")]
        public string Cpf { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define a senha.
        /// </summary>
        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "A senha deve ter de 8 a 64 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define se a pessoa aceitou a política de privacidade.
        /// </summary>
        [Range(typeof(bool), "true", "true", ErrorMessage = "É preciso aceitar a política de privacidade.")]
        public bool AceitePolitica { get; set; }
    }
}
