using System.Data;
using Dapper;

namespace Campanhas.Infrastructure.Leitura;

/// <summary>
/// Ensina o Dapper a enviar e a ler datas (<c>date</c> do PostgreSQL) como <see cref="DateOnly"/>, que ele não suporta por padrão.
/// </summary>
internal sealed class ConversorDeDateOnly : SqlMapper.TypeHandler<DateOnly>
{
    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    /// <inheritdoc/>
    public override DateOnly Parse(object value)
    {
        return value is DateOnly data ? data : DateOnly.FromDateTime((DateTime)value);
    }
}
