namespace Janatics.DataEngine.Infrastructure;

/// <summary>
/// Normalises provider names used by registry records and connection strings.
/// </summary>
public static class ProviderDetector
{
    public static string Normalize(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return "sqlite";
        }

        return providerName.Trim().ToLowerInvariant() switch
        {
            "sqlite" or "microsoft.data.sqlite" => "sqlite",
            "sqlserver" or "sql server" or "mssql" or "microsoft.data.sqlclient" => "sqlserver",
            "oracle" or "oracledb" or "oracle.manageddataaccess.client" => "oracle",
            _ => providerName.Trim().ToLowerInvariant()
        };
    }
}
