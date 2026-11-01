using System.Net;

namespace ConexaoSolidaria.Web.Servicos;

/// <summary>
/// Um pedaço do trecho da busca: o texto e se ele é o termo encontrado (destacado).
/// </summary>
/// <param name="Texto">Texto puro, sem marcação.</param>
/// <param name="Destaque">Verdadeiro se este pedaço estava entre <c>&lt;em&gt;</c>.</param>
public sealed record SegmentoDeTrecho(string Texto, bool Destaque);

/// <summary>
/// Converte o trecho da busca (texto com <c>&lt;em&gt;</c> em volta dos termos, com o resto do HTML já escapado pelo
/// Elasticsearch) em segmentos de texto puro. A tela desenha cada segmento como texto, então nenhuma outra tag chega ao
/// navegador, mesmo que a descrição da campanha tenha HTML: só a tag <c>em</c> é interpretada.
/// </summary>
public static class TrechoDestacado
{
    /// <summary>
    /// Abertura do destaque.
    /// </summary>
    private const string _abre = "<em>";

    /// <summary>
    /// Fechamento do destaque.
    /// </summary>
    private const string _fecha = "</em>";

    /// <summary>
    /// Separa o trecho em segmentos.
    /// </summary>
    /// <param name="trecho">Trecho devolvido pela busca; pode ser nulo.</param>
    /// <returns>Os segmentos, na ordem; vazio se não há trecho.</returns>
    public static IReadOnlyList<SegmentoDeTrecho> Segmentar(string? trecho)
    {
        var segmentos = new List<SegmentoDeTrecho>();
        if (string.IsNullOrEmpty(trecho))
        {
            return segmentos;
        }

        var posicao = 0;
        while (posicao < trecho.Length)
        {
            var abre = trecho.IndexOf(_abre, posicao, StringComparison.Ordinal);
            if (abre < 0)
            {
                Adicionar(segmentos, trecho[posicao..], false);
                break;
            }

            Adicionar(segmentos, trecho[posicao..abre], false);
            var inicioDoTermo = abre + _abre.Length;
            var fecha = trecho.IndexOf(_fecha, inicioDoTermo, StringComparison.Ordinal);
            if (fecha < 0)
            {
                Adicionar(segmentos, trecho[inicioDoTermo..], false);
                break;
            }

            Adicionar(segmentos, trecho[inicioDoTermo..fecha], true);
            posicao = fecha + _fecha.Length;
        }

        return segmentos;
    }

    /// <summary>
    /// Acrescenta um segmento, decodificando as entidades HTML que o Elasticsearch escapou, se ele não for vazio.
    /// </summary>
    /// <param name="segmentos">Lista em construção.</param>
    /// <param name="texto">Texto do segmento, ainda com entidades.</param>
    /// <param name="destaque">Se o segmento é um termo encontrado.</param>
    private static void Adicionar(List<SegmentoDeTrecho> segmentos, string texto, bool destaque)
    {
        if (texto.Length > 0)
        {
            segmentos.Add(new SegmentoDeTrecho(WebUtility.HtmlDecode(texto), destaque));
        }
    }
}
