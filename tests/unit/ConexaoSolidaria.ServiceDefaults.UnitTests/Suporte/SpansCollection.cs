using System.Collections;
using System.Diagnostics;

namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;

/// <summary>
/// Coleção de spans segura para escrita concorrente, usada com o exportador em memória do OpenTelemetry.
/// O <c>ActivitySource</c> do ASP.NET é global no processo, então testes em paralelo podem escrever na mesma lista.
/// </summary>
public sealed class SpansCollection : ICollection<Activity>
{
    /// <summary>
    /// Itens coletados.
    /// </summary>
    private readonly List<Activity> _itens = [];

    /// <summary>
    /// Trava que protege o acesso à lista.
    /// </summary>
    private readonly object _trava = new();

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            lock (_trava)
            {
                return _itens.Count;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Tira uma cópia dos spans coletados até agora.
    /// </summary>
    /// <returns>Lista imutável com os spans.</returns>
    public IReadOnlyList<Activity> Copia()
    {
        lock (_trava)
        {
            return [.. _itens];
        }
    }

    /// <summary>
    /// Indica se já foi coletado um span de servidor para o caminho informado.
    /// </summary>
    /// <param name="caminho">Caminho da requisição (tag <c>url.path</c>).</param>
    /// <returns><c>true</c> se o span existe.</returns>
    public bool TemCaminho(string caminho)
    {
        return Copia().Any(span => string.Equals(span.GetTagItem("url.path")?.ToString(), caminho, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public void Add(Activity item)
    {
        lock (_trava)
        {
            _itens.Add(item);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_trava)
        {
            _itens.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(Activity item)
    {
        lock (_trava)
        {
            return _itens.Contains(item);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(Activity[] array, int arrayIndex)
    {
        lock (_trava)
        {
            _itens.CopyTo(array, arrayIndex);
        }
    }

    /// <inheritdoc/>
    public bool Remove(Activity item)
    {
        lock (_trava)
        {
            return _itens.Remove(item);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<Activity> GetEnumerator()
    {
        return Copia().GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
