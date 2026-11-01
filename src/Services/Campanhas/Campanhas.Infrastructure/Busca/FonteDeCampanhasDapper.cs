using System.Runtime.CompilerServices;
using Campanhas.Application.Busca;
using Campanhas.Infrastructure.Leitura;
using Dapper;
using Npgsql;

namespace Campanhas.Infrastructure.Busca;

/// <summary>
/// Lê todas as campanhas do PostgreSQL em lotes (paginação por chave, sem <c>OFFSET</c>) para reconstruir o índice de busca.
/// A versão do documento é o instante da última alteração em ticks UTC, a mesma regra do <c>CampanhaAlteradaEvent</c>.
/// </summary>
/// <param name="fonte">Fonte de dados do PostgreSQL de campanhas.</param>
public sealed class FonteDeCampanhasDapper(NpgsqlDataSource fonte) : IFonteDeCampanhas
{
    /// <summary>
    /// Próximo lote: campanhas com identificador maior que o último já lido, em ordem de identificador.
    /// </summary>
    private const string _sqlLote = """
        SELECT id AS Id,
               titulo AS Titulo,
               descricao AS Descricao,
               status AS Status,
               meta_financeira AS MetaFinanceira,
               data_fim AS DataFim,
               atualizado_em AS AtualizadoEm
          FROM campanhas
         WHERE id > @Ultimo
         ORDER BY id
         LIMIT @Tamanho
        """;

    /// <summary>
    /// Registra o conversor de <see cref="DateOnly"/> do Dapper.
    /// </summary>
    static FonteDeCampanhasDapper()
    {
        SqlMapper.AddTypeHandler(new ConversorDeDateOnly());
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IReadOnlyList<CampanhaIndexada>> LerEmLotesAsync(int tamanhoDoLote, [EnumeratorCancellation] CancellationToken cancelamento)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoDoLote, 1);

        await using var conexao = await fonte.OpenConnectionAsync(cancelamento);
        var ultimo = Guid.Empty;
        while (true)
        {
            var linhas = (await conexao.QueryAsync<Linha>(new CommandDefinition(_sqlLote, new { Ultimo = ultimo, Tamanho = tamanhoDoLote }, cancellationToken: cancelamento))).AsList();
            if (linhas.Count == 0)
            {
                yield break;
            }

            ultimo = linhas[^1].Id;
            yield return [.. linhas.Select(l => new CampanhaIndexada(l.Id, l.Titulo, l.Descricao, l.Status, l.MetaFinanceira, l.DataFim, DateTime.SpecifyKind(l.AtualizadoEm, DateTimeKind.Utc).Ticks))];

            if (linhas.Count < tamanhoDoLote)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Linha lida da tabela <c>campanhas</c>.
    /// </summary>
    /// <param name="Id">Identificador da campanha.</param>
    /// <param name="Titulo">Título.</param>
    /// <param name="Descricao">Descrição.</param>
    /// <param name="Status">Situação.</param>
    /// <param name="MetaFinanceira">Meta financeira.</param>
    /// <param name="DataFim">Último dia.</param>
    /// <param name="AtualizadoEm">Instante da última alteração, em UTC.</param>
    private sealed record Linha(Guid Id, string Titulo, string Descricao, string Status, decimal MetaFinanceira, DateOnly DataFim, DateTime AtualizadoEm);
}
