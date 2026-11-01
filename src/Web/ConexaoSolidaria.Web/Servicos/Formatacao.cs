using System.Globalization;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Formatação de valores para a tela, sempre no padrão do Brasil (independe da cultura do servidor).
/// </summary>
public static class Formatacao
{
    /// <summary>
    /// Cultura pt-BR usada em todas as formatações.
    /// </summary>
    private static readonly CultureInfo _ptBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Formata um valor em reais, como <c>R$ 1.234,56</c>.
    /// </summary>
    /// <param name="valor">Valor em reais.</param>
    /// <returns>O texto do valor.</returns>
    public static string Moeda(decimal valor)
    {
        return valor.ToString("C2", _ptBr).Replace(' ', ' ');
    }

    /// <summary>
    /// Formata uma data como <c>dd/MM/aaaa</c>.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <returns>O texto da data.</returns>
    public static string Data(DateOnly data)
    {
        return data.ToString("dd/MM/yyyy", _ptBr);
    }

    /// <summary>
    /// Formata um percentual com até duas casas, como <c>1,50%</c>.
    /// </summary>
    /// <param name="percentual">Percentual (1,5 significa 1,5%).</param>
    /// <returns>O texto do percentual.</returns>
    public static string Percentual(decimal percentual)
    {
        return percentual.ToString("0.##", _ptBr) + "%";
    }
}
