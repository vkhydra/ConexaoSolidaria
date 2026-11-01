using Microsoft.AspNetCore.Components.Web;

namespace ConexaoSolidaria.Web;

/// <summary>
/// Modos de renderização compartilhados pelas páginas interativas.
/// </summary>
public static class ModosDeRenderizacao
{
    /// <summary>
    /// Interativo no servidor, sem pré-renderização: a página carrega os dados uma única vez, já no circuito, onde a sessão do
    /// usuário (cookie) está disponível, em vez de repetir as chamadas às APIs na pré-renderização.
    /// </summary>
    public static readonly InteractiveServerRenderMode InterativoSemPrerender = new(prerender: false);
}
