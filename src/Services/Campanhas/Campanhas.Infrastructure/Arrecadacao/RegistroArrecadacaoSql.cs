using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using Dapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Campanhas.Infrastructure.Arrecadacao;

/// <summary>
/// Soma uma doação ao total da campanha em uma única transação do PostgreSQL: registro em <c>doacoes_processadas</c>
/// (a chave primária garante uma soma por doação), <c>UPDATE</c> atômico do total e gravação do <c>DoacaoProcessadaEvent</c> no
/// Outbox do MassTransit. Se a doação já foi somada, ou se a campanha não existe, nada é alterado. Quando já existe uma
/// transação no contexto (a do Outbox do consumidor), ela é reaproveitada e quem a abriu é quem a confirma.
/// </summary>
/// <param name="contexto">Contexto do banco de campanhas, de onde vêm a conexão e a transação.</param>
/// <param name="publicador">Publicador do MassTransit, que grava na tabela <c>outbox_message</c>.</param>
public sealed class RegistroArrecadacaoSql(CampanhasDbContext contexto, IPublishEndpoint publicador) : IRegistroArrecadacao
{
    /// <summary>
    /// Registra a doação e soma o valor ao total num só comando: o <c>INSERT ... ON CONFLICT DO NOTHING</c> não lança exceção
    /// para a doação repetida (o que abortaria a transação) e só ocorre se a campanha existe; o <c>UPDATE</c> só soma quando o
    /// <c>INSERT</c> gravou uma linha. Sem linha de retorno, a doação é repetida ou a campanha não existe.
    /// </summary>
    private const string _sqlRegistrarESomar = """
        WITH nova AS (
            INSERT INTO doacoes_processadas (doacao_id, campanha_id, valor, processada_em)
            SELECT @DoacaoId, @CampanhaId, @Valor, @Agora
            WHERE EXISTS (SELECT 1 FROM campanhas WHERE id = @CampanhaId)
            ON CONFLICT (doacao_id) DO NOTHING
            RETURNING doacao_id
        )
        UPDATE campanhas
           SET total_arrecadado = total_arrecadado + @Valor, atualizado_em = @Agora
         WHERE id = @CampanhaId AND EXISTS (SELECT 1 FROM nova)
        RETURNING total_arrecadado
        """;

    /// <summary>
    /// Lê o instante da primeira soma e o total atual da campanha, para responder a uma reentrega.
    /// </summary>
    private const string _sqlLerJaProcessada =
        "SELECT d.processada_em AS ProcessadaEm, c.total_arrecadado AS TotalArrecadado FROM doacoes_processadas d JOIN campanhas c ON c.id = d.campanha_id WHERE d.doacao_id = @DoacaoId";

    /// <inheritdoc/>
    public async Task<ResultadoRegistro> RegistrarAsync(DoacaoRecebidaEvent doacao, DateTimeOffset agora, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(doacao);

        var transacaoExistente = contexto.Database.CurrentTransaction;
        await using var transacaoPropria = transacaoExistente is null ? await contexto.Database.BeginTransactionAsync(cancelamento) : null;
        var transacao = transacaoExistente ?? transacaoPropria!;
        var conexao = contexto.Database.GetDbConnection();
        var transacaoDb = transacao.GetDbTransaction();
        var parametros = new { doacao.DoacaoId, doacao.CampanhaId, doacao.Valor, Agora = agora.ToUniversalTime() };

        var total = await conexao.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(_sqlRegistrarESomar, parametros, transacaoDb, cancellationToken: cancelamento));
        if (total is null)
        {
            var anterior = await conexao.QuerySingleOrDefaultAsync<LinhaJaProcessada>(
                new CommandDefinition(_sqlLerJaProcessada, new { doacao.DoacaoId }, transacaoDb, cancellationToken: cancelamento));
            if (transacaoPropria is not null)
            {
                await transacaoPropria.RollbackAsync(cancelamento);
            }

            return anterior is null
                ? new ResultadoRegistro(SituacaoRegistro.CampanhaInexistente, null, null)
                : new ResultadoRegistro(SituacaoRegistro.JaProcessada, new DateTimeOffset(DateTime.SpecifyKind(anterior.ProcessadaEm, DateTimeKind.Utc)), anterior.TotalArrecadado);
        }

        await publicador.Publish(
            new DoacaoProcessadaEvent(doacao.DoacaoId, doacao.CampanhaId, doacao.DoadorId, doacao.Valor, total.Value, doacao.OcorridoEm, agora),
            cancelamento);

        if (transacaoPropria is not null)
        {
            await contexto.SaveChangesAsync(cancelamento);
            await transacaoPropria.CommitAsync(cancelamento);
        }

        return new ResultadoRegistro(SituacaoRegistro.Registrada, agora, total.Value);
    }

    /// <summary>
    /// Linha lida da consulta de reentrega.
    /// </summary>
    /// <param name="ProcessadaEm">Instante (UTC) da primeira soma.</param>
    /// <param name="TotalArrecadado">Total atual da campanha.</param>
    private sealed record LinhaJaProcessada(DateTime ProcessadaEm, decimal TotalArrecadado);
}
