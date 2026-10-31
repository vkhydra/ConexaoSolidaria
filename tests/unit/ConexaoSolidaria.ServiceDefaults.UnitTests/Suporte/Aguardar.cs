namespace ConexaoSolidaria.ServiceDefaults.UnitTests.Suporte;

/// <summary>
/// Espera, com limite de tempo, por uma condição que acontece em segundo plano.
/// Evita <c>Thread.Sleep</c> nos testes.
/// </summary>
public static class Aguardar
{
    /// <summary>
    /// Verifica a condição repetidamente até ela ser verdadeira ou o tempo acabar.
    /// </summary>
    /// <param name="condicao">Condição esperada.</param>
    /// <param name="limite">Tempo máximo de espera.</param>
    /// <param name="cancelamento">Token de cancelamento do teste.</param>
    /// <returns><c>true</c> se a condição ficou verdadeira dentro do limite.</returns>
    public static async Task<bool> AteAsync(Func<bool> condicao, TimeSpan limite, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(condicao);

        using var cronometro = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
        cronometro.CancelAfter(limite);
        try
        {
            while (!condicao())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20), cronometro.Token);
            }

            return true;
        }
        catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested)
        {
            return condicao();
        }
    }
}
