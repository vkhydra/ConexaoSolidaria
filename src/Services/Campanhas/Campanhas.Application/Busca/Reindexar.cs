using Campanhas.Domain.Comum;
using Microsoft.Extensions.Logging;

namespace Campanhas.Application.Busca;

/// <summary>
/// Caso de uso de reindexação: recria o índice de busca a partir do PostgreSQL sem tirar a busca do ar. Cria um índice novo,
/// carrega as campanhas em lotes de 500 e troca o alias ao final. Roda em segundo plano, uma reindexação por vez neste processo.
/// Alterações feitas durante a carga vão para o índice antigo; se importar, basta reindexar de novo.
/// </summary>
/// <param name="fonte">Leitura das campanhas do banco.</param>
/// <param name="indice">Administração do índice de busca.</param>
/// <param name="log">Logger do caso de uso.</param>
public sealed partial class Reindexar(IFonteDeCampanhas fonte, IGerenciadorDeIndice indice, ILogger<Reindexar> log)
{
    /// <summary>
    /// Quantidade de campanhas lidas e gravadas por lote.
    /// </summary>
    public const int TamanhoDoLote = 500;

    /// <summary>
    /// Indica, com 1, que há uma reindexação em andamento.
    /// </summary>
    private int _emAndamento;

    /// <summary>
    /// Obtém a tarefa da última reindexação iniciada, para os testes e para esperar o fim no desligamento; nula se nenhuma foi iniciada.
    /// A tarefa nunca falha: erros são registrados no log.
    /// </summary>
    public Task? Execucao { get; private set; }

    /// <summary>
    /// Inicia a reindexação em segundo plano e devolve na hora.
    /// </summary>
    /// <returns>Sucesso se a reindexação começou, ou <c>busca.reindexacao_em_andamento</c> se outra ainda roda.</returns>
    public Result Iniciar()
    {
        if (Interlocked.CompareExchange(ref _emAndamento, 1, 0) != 0)
        {
            return Result.Falha(ErrosDeBusca.ReindexacaoEmAndamento);
        }

        Execucao = Task.Run(ExecutarAsync);
        return Result.Ok();
    }

    /// <summary>
    /// Faz a reindexação e libera a trava ao terminar, com sucesso ou não. Se algo falha, o índice novo é apagado e o alias
    /// continua apontando para o índice antigo.
    /// </summary>
    /// <returns>Tarefa que termina com a reindexação concluída ou abandonada.</returns>
    private async Task ExecutarAsync()
    {
        string? novoIndice = null;
        try
        {
            novoIndice = await indice.CriarIndiceNovoAsync(CancellationToken.None);
            var total = 0;
            await foreach (var lote in fonte.LerEmLotesAsync(TamanhoDoLote, CancellationToken.None))
            {
                await indice.GravarLoteAsync(novoIndice, lote, CancellationToken.None);
                total += lote.Count;
            }

            await indice.PublicarIndiceAsync(novoIndice, CancellationToken.None);
            LogConcluida(log, novoIndice, total);
        }
        catch (Exception excecao)
        {
            LogFalhou(log, excecao.GetType().Name, novoIndice);
            if (novoIndice is not null)
            {
                await DescartarSemFalharAsync(novoIndice);
            }
        }
        finally
        {
            Volatile.Write(ref _emAndamento, 0);
        }
    }

    /// <summary>
    /// Tenta apagar o índice novo de uma reindexação que falhou; se nem isso der, só registra.
    /// </summary>
    /// <param name="novoIndice">Nome do índice a apagar.</param>
    /// <returns>Tarefa que termina depois da tentativa.</returns>
    private async Task DescartarSemFalharAsync(string novoIndice)
    {
        try
        {
            await indice.DescartarIndiceAsync(novoIndice, CancellationToken.None);
        }
        catch (Exception excecao)
        {
            LogDescarteFalhou(log, excecao.GetType().Name, novoIndice);
        }
    }

    /// <summary>
    /// Registra a reindexação concluída.
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <param name="indice">Nome do índice publicado.</param>
    /// <param name="total">Quantidade de campanhas indexadas.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Reindexação concluída: índice {Indice} com {Total} campanhas.")]
    private static partial void LogConcluida(ILogger log, string indice, int total);

    /// <summary>
    /// Registra a falha da reindexação, sem a mensagem da exceção (que pode trazer dados da consulta).
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <param name="tipoDaExcecao">Tipo da exceção.</param>
    /// <param name="indice">Nome do índice novo, se já tinha sido criado.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "A reindexação falhou ({TipoDaExcecao}); o índice novo {Indice} será descartado e o antigo continua em uso.")]
    private static partial void LogFalhou(ILogger log, string tipoDaExcecao, string? indice);

    /// <summary>
    /// Registra a falha ao apagar o índice novo depois de uma reindexação que falhou.
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <param name="tipoDaExcecao">Tipo da exceção.</param>
    /// <param name="indice">Nome do índice que ficou para trás.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível descartar o índice {Indice} ({TipoDaExcecao}); apague-o manualmente.")]
    private static partial void LogDescarteFalhou(ILogger log, string tipoDaExcecao, string indice);
}
