using Dapper;

namespace Janatics.DataEngine.Builders;

/// <summary>
/// Represents a parameterised SQL statement and any supporting SQL.
/// </summary>
public sealed record SqlStatement(
    string Sql,
    DynamicParameters Parameters,
    string? CountSql = null,
    string? IdentitySql = null);
