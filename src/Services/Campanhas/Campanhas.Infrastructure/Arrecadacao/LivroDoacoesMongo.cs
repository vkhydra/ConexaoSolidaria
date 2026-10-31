using System.Diagnostics;
using Campanhas.Application.Arrecadacao;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Campanhas.Infrastructure.Arrecadacao;

/// <summary>
/// Livro de doações no MongoDB (banco <c>arrecadacao</c>, coleção <c>lancamentos_doacao</c>). Cada doação tem um único
/// lançamento, com <c>_id</c> igual ao identificador da doação, gravado por upsert com <c>$setOnInsert</c>: uma reentrega
/// recria o lançamento que faltava, mas nunca altera um que já existe. Valores monetários ficam em <c>Decimal128</c>.
/// </summary>
/// <param name="banco">Banco <c>arrecadacao</c> do MongoDB.</param>
public sealed class LivroDoacoesMongo(IMongoDatabase banco) : ILivroDoacoes
{
    /// <summary>
    /// Nome do banco no MongoDB.
    /// </summary>
    public const string NomeBanco = "arrecadacao";

    /// <summary>
    /// Nome da coleção dos lançamentos.
    /// </summary>
    public const string NomeColecao = "lancamentos_doacao";

    /// <summary>
    /// Versão do esquema do documento, para evoluções futuras.
    /// </summary>
    public const int VersaoEsquema = 1;

    /// <summary>
    /// Indica se o índice já foi criado por esta instância; criá-lo de novo é inofensivo, mas evita uma ida ao servidor por mensagem.
    /// </summary>
    private volatile bool _indiceGarantido;

    /// <inheritdoc/>
    public async Task RegistrarAsync(LancamentoDoacao lancamento, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(lancamento);

        var colecao = banco.GetCollection<BsonDocument>(NomeColecao);
        await GarantirIndiceAsync(colecao, cancelamento);

        var id = lancamento.DoacaoId.ToString();
        var documento = new BsonDocument
        {
            { "campanhaId", lancamento.CampanhaId.ToString() },
            { "doadorId", lancamento.DoadorId.ToString() },
            { "valor", new BsonDecimal128(lancamento.Valor) },
            { "totalAposLancamento", new BsonDecimal128(lancamento.TotalAposLancamento) },
            { "recebidaEm", new BsonDateTime(lancamento.RecebidaEm.UtcDateTime) },
            { "processadaEm", new BsonDateTime(lancamento.ProcessadaEm.UtcDateTime) },
            { "traceId", Activity.Current?.TraceId.ToString() is { } traceId ? new BsonString(traceId) : BsonNull.Value },
            { "versaoEsquema", VersaoEsquema },
        };

        await colecao.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", id),
            new BsonDocument("$setOnInsert", documento),
            new UpdateOptions { IsUpsert = true },
            cancelamento);
    }

    /// <summary>
    /// Cria o índice <c>{ campanhaId: 1, processadaEm: -1 }</c>, usado pelas conferências por campanha.
    /// </summary>
    /// <param name="colecao">Coleção dos lançamentos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Tarefa que termina quando o índice existe.</returns>
    private async Task GarantirIndiceAsync(IMongoCollection<BsonDocument> colecao, CancellationToken cancelamento)
    {
        if (_indiceGarantido)
        {
            return;
        }

        var chaves = Builders<BsonDocument>.IndexKeys.Ascending("campanhaId").Descending("processadaEm");
        await colecao.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(chaves, new CreateIndexOptions { Name = "ix_campanha_processada_em" }), cancellationToken: cancelamento);
        _indiceGarantido = true;
    }
}
