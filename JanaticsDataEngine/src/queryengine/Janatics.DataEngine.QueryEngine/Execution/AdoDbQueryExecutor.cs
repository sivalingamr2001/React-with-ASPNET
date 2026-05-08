using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Exceptions;
using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.Providers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.QueryEngine.Execution;

public sealed class AdoDbQueryExecutor(
    IDbProviderFactory providerFactory,
    IConnectionStringResolver connectionStringResolver,
    ILogger<AdoDbQueryExecutor> logger) : IDbQueryExecutor
{
    public async Task<IReadOnlyList<IDictionary<string, object?>>> ExecuteAsync(
        QueryDefinition definition,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
    {
        try
        {
            var provider = providerFactory.Resolve(definition.ProviderType);
            var connectionString = connectionStringResolver.Resolve(definition.ConnectionName);
            using var connection = await provider.OpenReadConnectionAsync(connectionString, ct).ConfigureAwait(false);
            using var command = connection.CreateCommand();

            command.CommandText = definition.SqlTemplate;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = provider.Capabilities.DefaultCommandTimeoutSeconds;

            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = $"{provider.Dialect.ParameterPrefix}{parameter.Key}";
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }

            logger.LogDebug("Executing query {QueryKey} using {ProviderType}.", definition.QueryKey, definition.ProviderType);

            using var reader = await ExecuteReaderAsync(command, ct).ConfigureAwait(false);
            var rows = new List<IDictionary<string, object?>>();

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct).ConfigureAwait(false)
                        ? null
                        : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {
            throw new QueryExecutionException($"Failed to execute query '{definition.QueryKey}'.", ex);
        }
    }

    private static async Task<DbDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken ct)
    {
        if (command is DbCommand dbCommand)
        {
            return await dbCommand.ExecuteReaderAsync(ct).ConfigureAwait(false);
        }

        return (DbDataReader)command.ExecuteReader()!;
    }
}
