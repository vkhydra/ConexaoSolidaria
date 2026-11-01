using System.Globalization;

namespace ConexaoSolidaria.ServiceDefaults.Saude;

/// <summary>
/// Sonda de saúde para o <c>HEALTHCHECK</c> das imagens: a imagem <c>aspnet</c> não tem <c>curl</c>, então o próprio aplicativo,
/// iniciado com <c>--healthcheck</c>, chama <c>/health/live</c> de si mesmo e termina com código 0 (saudável) ou 1.
/// </summary>
public static class SondaDeContainer
{
    /// <summary>
    /// Argumento de linha de comando que pede a sonda em vez de iniciar o servidor.
    /// </summary>
    public const string Argumento = "--healthcheck";

    /// <summary>
    /// Porta usada quando nenhuma variável do ASP.NET a define.
    /// </summary>
    public const int PortaPadrao = 8080;

    /// <summary>
    /// Indica se os argumentos pedem a sonda.
    /// </summary>
    /// <param name="argumentos">Argumentos do processo.</param>
    /// <returns><c>true</c> se <c>--healthcheck</c> está presente.</returns>
    public static bool Solicitada(string[] argumentos)
    {
        ArgumentNullException.ThrowIfNull(argumentos);

        return argumentos.Contains(Argumento, StringComparer.Ordinal);
    }

    /// <summary>
    /// Descobre a porta em que o servidor escuta: a primeira de <c>ASPNETCORE_HTTP_PORTS</c>, senão a primeira URL de
    /// <c>ASPNETCORE_URLS</c>, senão <see cref="PortaPadrao"/>.
    /// </summary>
    /// <param name="portasHttp">Valor de <c>ASPNETCORE_HTTP_PORTS</c>.</param>
    /// <param name="urls">Valor de <c>ASPNETCORE_URLS</c>.</param>
    /// <returns>A porta a consultar.</returns>
    public static int DescobrirPorta(string? portasHttp, string? urls)
    {
        var primeiraPorta = portasHttp?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (int.TryParse(primeiraPorta, NumberStyles.None, CultureInfo.InvariantCulture, out var porta))
        {
            return porta;
        }

        var primeiraUrl = urls?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        var posicao = primeiraUrl?.LastIndexOf(':') ?? -1;
        return posicao >= 0 && int.TryParse(primeiraUrl![(posicao + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out porta)
            ? porta
            : PortaPadrao;
    }

    /// <summary>
    /// Descobre a porta pelas variáveis de ambiente do processo e executa a sonda com um <see cref="HttpClient"/> de 2 segundos.
    /// </summary>
    /// <returns>0 se <c>/health/live</c> respondeu com sucesso; 1 nos demais casos.</returns>
    public static async Task<int> ExecutarAsync()
    {
        using var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var porta = DescobrirPorta(
            Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"),
            Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));

        return await ExecutarAsync(porta, cliente);
    }

    /// <summary>
    /// Chama <c>/health/live</c> em <c>localhost</c> na porta indicada.
    /// </summary>
    /// <param name="porta">Porta do servidor.</param>
    /// <param name="cliente">Cliente HTTP usado na chamada.</param>
    /// <returns>0 se a resposta foi de sucesso; 1 se foi de erro ou se a conexão falhou.</returns>
    public static async Task<int> ExecutarAsync(int porta, HttpClient cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        try
        {
            using var resposta = await cliente.GetAsync(new Uri($"http://localhost:{porta}{ExtensoesSaude.RotaVida}"));
            return resposta.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
