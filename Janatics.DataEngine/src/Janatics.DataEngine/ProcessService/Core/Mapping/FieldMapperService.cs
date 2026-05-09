using Janatics.DataEngine.ProcessService.Core.Auditing;
using Janatics.DataEngine.ProcessService.Models.Metadata;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Janatics.DataEngine.ProcessService.Core.Mapping;

public class FieldMapperService(IDataProvider configDataProvider, ILogger<FieldMapperService> logger)
{
    private readonly IDataProvider _configDataProvider = configDataProvider;
    private readonly ILogger<FieldMapperService> _logger = logger;

    public async Task<List<FieldMapperModel>> GetFieldMappersAsync(string entityName, IDbTransaction? transaction = null)
    {
        try
        {
            var sql = @"
                    SELECT EntityName, FieldName, ColumnName, DataType, IsActive, Properties, DefaultValue, allowupdate, displayname
                    FROM FieldMapper 
                    WHERE EntityName = @EntityName AND IsActive = @IsActive
                    ORDER BY FieldName";

            var parameters = new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "IsActive", true }
            };

            DataTable dataTable;
            try
            {
                dataTable = await _configDataProvider.ExecuteQueryAsync(sql, parameters, transaction);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FieldMapper query with displayname failed for {EntityName}. Falling back to legacy query without displayname.", entityName);
                var legacySql = @"
                        SELECT EntityName, FieldName, ColumnName, DataType, IsActive, Properties, DefaultValue, allowupdate
                        FROM FieldMapper 
                        WHERE EntityName = @EntityName AND IsActive = @IsActive
                        ORDER BY FieldName";
                dataTable = await _configDataProvider.ExecuteQueryAsync(legacySql, parameters, transaction);
            }
            var mappers = new List<FieldMapperModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                mappers.Add(new FieldMapperModel
                {
                    EntityName = row["EntityName"].ToString() ?? "",
                    FieldName = row["FieldName"].ToString() ?? "",
                    ColumnName = row["ColumnName"].ToString() ?? "",
                    DisplayName = dataTable.Columns.Contains("displayname")
                        ? row["displayname"] == DBNull.Value ? null : row["displayname"]?.ToString()
                        : dataTable.Columns.Contains("DisplayName") ? row["DisplayName"] == DBNull.Value ? null : row["DisplayName"]?.ToString() : null,
                    DataType = row["DataType"].ToString() ?? "",
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    Properties = row["Properties"].ToString() ?? "",
                    AllowUpdate= row["allowupdate"] == DBNull.Value || (bool)row["allowupdate"],
                    DefaultValue = row["DefaultValue"] == DBNull.Value ? null : row["DefaultValue"].ToString()
                });
            }

            _logger.LogInformation("Retrieved {Count} field mappers for entity {EntityName}", mappers.Count, entityName);
            return mappers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve field mappers for entity {EntityName}", entityName);
            throw;
        }
    }
}