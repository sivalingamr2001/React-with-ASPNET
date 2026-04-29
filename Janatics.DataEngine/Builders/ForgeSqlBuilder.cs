using Dapper;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Registry;

namespace Janatics.DataEngine.Builders;

/// <summary>
/// Builds provider-aware INSERT, UPDATE and soft-delete statements.
/// </summary>
public sealed class ForgeSqlBuilder
{
    private readonly SqlDialect _dialect;

    public ForgeSqlBuilder(SqlDialect dialect)
    {
        _dialect = dialect;
    }

    public SqlStatement BuildCreate(EntityMeta entityMeta, IReadOnlyDictionary<string, object?> data)
    {
        if (data.Count == 0)
        {
            throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires at least one value for create.");
        }

        var parameters = new DynamicParameters();
        var columns = new List<string>();
        var values = new List<string>();
        var index = 0;

        foreach (var pair in data)
        {
            var parameterName = $"p{index++}";
            columns.Add(_dialect.QuoteIdentifier(pair.Key));
            values.Add(_dialect.Parameter(parameterName));
            parameters.Add(parameterName, pair.Value);
        }

        var sql = $"INSERT INTO {_dialect.QuoteIdentifier(entityMeta.QualifiedTableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
        var needsIdentity = !data.ContainsKey(entityMeta.PrimaryKey);
        var identitySql = needsIdentity && ProviderDetector.Normalize(_dialect.ProviderName) == "sqlite"
            ? "SELECT last_insert_rowid();"
            : null;

        return new SqlStatement(sql, parameters, IdentitySql: identitySql);
    }

    public SqlStatement BuildUpdate(EntityMeta entityMeta, IReadOnlyDictionary<string, object?> data, object keyValue)
    {
        if (data.Count == 0)
        {
            throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires at least one updatable value for update.");
        }

        var parameters = new DynamicParameters();
        var assignments = new List<string>();
        var index = 0;

        foreach (var pair in data)
        {
            var parameterName = $"p{index++}";
            assignments.Add($"{_dialect.QuoteIdentifier(pair.Key)} = {_dialect.Parameter(parameterName)}");
            parameters.Add(parameterName, pair.Value);
        }

        parameters.Add("pk", keyValue);
        var sql = $"UPDATE {_dialect.QuoteIdentifier(entityMeta.QualifiedTableName)} SET {string.Join(", ", assignments)} WHERE {_dialect.QuoteIdentifier(entityMeta.PrimaryKey)} = {_dialect.Parameter("pk")}";
        return new SqlStatement(sql, parameters);
    }

    public SqlStatement BuildSoftDelete(EntityMeta entityMeta, object keyValue)
    {
        if (string.IsNullOrWhiteSpace(entityMeta.SoftDeleteColumn))
        {
            throw new SqlBuildException($"Entity '{entityMeta.EntityName}' does not define a soft delete column.");
        }

        var parameters = new DynamicParameters();
        parameters.Add("deleted", 1);
        parameters.Add("pk", keyValue);
        var sql = $"UPDATE {_dialect.QuoteIdentifier(entityMeta.QualifiedTableName)} SET {_dialect.QuoteIdentifier(entityMeta.SoftDeleteColumn)} = {_dialect.Parameter("deleted")} WHERE {_dialect.QuoteIdentifier(entityMeta.PrimaryKey)} = {_dialect.Parameter("pk")}";
        return new SqlStatement(sql, parameters);
    }
}
