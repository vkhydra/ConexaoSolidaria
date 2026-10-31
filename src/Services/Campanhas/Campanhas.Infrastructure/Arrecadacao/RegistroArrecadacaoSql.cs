using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using Dapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Campanhas.Infrastructure.Arrecadacao;

/// <summary>
/// Soma uma doação ao total da campanha em uma única transação do PostgreSQL: <c>INSERT</c> em <c>doacoes_processadas</c>
/// (a chave primária garante uma soma por doação), <c>UPDATE</c> atômico do total e gravação do <c>DoacaoProcessadaEvent</c> no
/// Outbox do MassTransit. Se a doação já foi somada, ou se a campanha não existe, nada é alterado.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas, de onde vêm a conexão e a transação.</param>
/// <param name="publicador">Publicador do MassTransit, que no modo bus outbox grava na tabela <c>outbox_message</c>.</param>
public sealed class RegistroArrecadacaoSql(CampanhasDbContext contexto, IPublishEndpoint publicador) : IRegistroArrecadacao
{
    /// <summary>
    /// Registra a doação processada; a chave primária duplicada indica reentrega.
    /// </summary>
    private const string _sqlInserirDoacao =
        "INSERT INTO doacoes_processadas (doacao_id, campanha_id, valor, processada_em) VALUES (@DoacaoId, @CampanhaId, @Valor, @Agora)";

    /// <summary>
    /// Soma o valor ao total sem carregar o agregado e devolve o novo total.
    /// </summary>
    private const string _sqlSomarAoTotal =
        "UPDATE campanhas SET total_arrecadado = total_arrecadado + @Valor, atualizado_em = @Agora WHERE id = @CampanhaId RETURNING total_arrecadado";

    /// <summary>
    /// Lê o instante da primeira soma e o total atual da campanha, para responder a uma reentrega.
    /// </summary>
    private const string _sqlLerJaProcessada =
        "SELECT d.processada_em AS ProcessadaEm, c.total_arrecadado AS TotalArrecadado FROM doacoes_processadas d JOIN campanhas c ON c.id = d.campanha_id WHERE d.doacao_id = @DoacaoId";

    /// <inheritdoc/>
    public async Task<ResultadoRegistro> RegistrarAsync(DoacaoRecebidaEvent doacao, DateTimeOffset agora, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(doacao);

        var parametros = new { doacao.DoacaoId, doacao.CampanhaId, doacao.Valor, Agora = agora.ToUniversalTime() };
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);
        var conexao = contexto.Database.GetDbConnection();
        var transacaoDb = transacao.GetDbTransaction();

        try
        {
            await conexao.ExecuteAsync(new CommandDefinition(_sqlInserirDoacao, parametros, transacaoDb, cancellationToken: cancelamento));
        }
        catch (PostgresException excecao) when (excecao.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transacao.RollbackAsync(cancelamento);
            return await LerJaProcessadaAsync(conexao, doacao.DoacaoId, cancelamento);
        }
        catch (PostgresException excecao) when (excecao.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await transacao.RollbackAsync(cancelamento);
            return new ResultadoRegistro(SituacaoRegistro.CampanhaInexistente, null, null);
        }

        var total = await conexao.QuerySingleAsync<decimal>(new CommandDefinition(_sqlSomarAoTotal, parametros, transacaoDb, cancellationToken: cancelamento));

        await publicador.Publish(
            new DoacaoProcessadaEvent(doacao.DoacaoId, doacao.CampanhaId, doacao.DoadorId, doacao.Valor, total, doacao.OcorridoEm, agora),
            cancelamento);
        await contexto.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        return new ResultadoRegistro(SituacaoRegistro.Registrada, agora, total);
    }

    /// <summary>
    /// Lê os dados da doação já somada, fora de transação, depois do desfazer da tentativa.
    /// </summary>
    /// <param name="conexao">Conexão do contexto.</param>
    /// <param name="doacaoId">Identificador da doação.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O resultado de reentrega, com o instante da primeira soma e o total atual.</returns>
    private static async Task<ResultadoRegistro> LerJaProcessadaAsync(System.Data.Common.DbConnection conexao, Guid doacaoId, CancellationToken cancelamento)
    {
        var linha = await conexao.QuerySingleAsync<LinhaJaProcessada>(new CommandDefinition(_sqlLerJaProcessada, new { DoacaoId = doacaoId }, cancellationToken: cancelamento));
        return new ResultadoRegistro(SituacaoRegistro.JaProcessada, new DateTimeOffset(DateTime.SpecifyKind(linha.ProcessadaEm, DateTimeKind.Utc)), linha.TotalArrecadado);
    }

    /// <summary>
    /// Linha lida da consulta de reentrega.
    /// </summary>
    /// <param name="ProcessadaEm">Instante (UTC) da primeira soma.</param>
    /// <param name="TotalArrecadado">Total atual da campanha.</param>
    private sealed record LinhaJaProcessada(DateTime ProcessadaEm, decimal TotalArrecadado);
}
