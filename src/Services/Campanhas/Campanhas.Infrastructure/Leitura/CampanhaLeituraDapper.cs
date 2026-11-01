using Campanhas.Application.Transparencia;
using Dapper;
using Npgsql;

namespace Campanhas.Infrastructure.Leitura;

/// <summary>
/// Consulta do painel com Dapper: lado de leitura enxuto, direto em SQL, sem carregar o agregado nem rastrear entidades.
/// </summary>
/// <param name="fonte">Fonte de dados do PostgreSQL de campanhas.</param>
public sealed class CampanhaLeituraDapper(NpgsqlDataSource fonte) : ICampanhaLeitura
{
    /// <summary>
    /// Consulta do painel: ativas cujo período inclui o dia, mais próximas do fim primeiro, até 100.
    /// </summary>
    private const string _sqlPainel = """
        SELECT id AS Id,
               titulo AS Titulo,
               meta_financeira AS MetaFinanceira,
               total_arrecadado AS TotalArrecadado,
               data_fim AS DataFim
          FROM campanhas
         WHERE status = 'Ativa' AND data_inicio <= @Hoje AND data_fim >= @Hoje
         ORDER BY data_fim, id
         LIMIT 100
        """;

    /// <summary>
    /// Registra o conversor de <see cref="DateOnly"/> do Dapper, que não o suporta por padrão.
    /// </summary>
    static CampanhaLeituraDapper()
    {
        SqlMapper.AddTypeHandler(new ConversorDeDateOnly());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CampanhaDoPainel>> ListarAtivasAsync(DateOnly hoje, CancellationToken cancelamento)
    {
        await using var conexao = await fonte.OpenConnectionAsync(cancelamento);
        var linhas = await conexao.QueryAsync<CampanhaDoPainel>(new CommandDefinition(_sqlPainel, new { Hoje = hoje }, cancellationToken: cancelamento));
        return [.. linhas];
    }
}
