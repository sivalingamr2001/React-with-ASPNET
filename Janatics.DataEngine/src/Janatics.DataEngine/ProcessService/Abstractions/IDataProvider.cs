using Npgsql;
using System.Data;

namespace Janatics.DataEngine.Core.Auditing;

public interface IDataProvider
{
    Task<IDbConnection> GetConnectionAsync();
    Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection);
    Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction);
    Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction);
    Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null);
    Task<object?> ExecuteInsertWithReturnAsync(string tableName, Dictionary<string, object> parameters, string returnColumn, IDbTransaction transaction);
    Task<int> ExecuteUpdateAsync(string tableName, Dictionary<string, object> parameters, Dictionary<string, object> whereConditions, IDbTransaction transaction);
    Task<int> ExecuteDeleteAsync(string tableName, Dictionary<string, object> whereConditions, IDbTransaction transaction);
    string GetParameterPlaceholder(string parameterName);
    string FormatTableName(string tableName);
    Task<int> ExecuteNonQueryAsync(string sql, NpgsqlCommand command);
    Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction transaction);
}