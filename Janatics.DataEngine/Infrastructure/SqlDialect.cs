using Janatics.DataEngine.Exceptions;

namespace Janatics.DataEngine.Infrastructure;

/// <summary>
/// Encapsulates provider-specific SQL syntax.
/// </summary>
public sealed class SqlDialect
{
    private SqlDialect(
        string providerName,
        string openQuote,
        string closeQuote,
        string parameterPrefix)
    {
        ProviderName = providerName;
        OpenQuote = openQuote;
        CloseQuote = closeQuote;
        ParameterPrefix = parameterPrefix;
    }

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets the provider parameter prefix.
    /// </summary>
    public string ParameterPrefix { get; }

    public string OpenQuote { get; }

    public string CloseQuote { get; }

    public static SqlDialect ForProvider(string providerName)
    {
        return ProviderDetector.Normalize(providerName) switch
        {
            "sqlite" => new SqlDialect("Sqlite", "\"", "\"", "@"),
            "sqlserver" => new SqlDialect("SqlServer", "[", "]", "@"),
            "oracle" => new SqlDialect("Oracle", "\"", "\"", ":"),
            _ => throw new SqlBuildException($"Provider '{providerName}' is not supported.")
        };
    }

    public string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new SqlBuildException("Identifier cannot be empty.");
        }

        var segments = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(static segment => segment.Length == 0))
        {
            throw new SqlBuildException($"Invalid identifier '{identifier}'.");
        }

        return string.Join('.', segments.Select(QuoteSingle));
    }

    public string Parameter(string name)
    {
        var cleaned = name.Trim().TrimStart('@', ':');
        return $"{ParameterPrefix}{cleaned}";
    }

    public string ApplyPaging(string sql, string offsetParameterName, string limitParameterName)
    {
        var offset = Parameter(offsetParameterName);
        var limit = Parameter(limitParameterName);

        return ProviderDetector.Normalize(ProviderName) switch
        {
            "sqlite" => $"{sql} LIMIT {limit} OFFSET {offset}",
            "sqlserver" => $"{sql} OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY",
            "oracle" => $"{sql} OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY",
            _ => throw new SqlBuildException($"Provider '{ProviderName}' is not supported.")
        };
    }

    private string QuoteSingle(string identifier)
    {
        var trimmed = identifier.Trim();
        return ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"{OpenQuote}{trimmed.Replace("]", "]]", StringComparison.Ordinal)}{CloseQuote}"
            : $"{OpenQuote}{trimmed.Replace(CloseQuote, CloseQuote + CloseQuote, StringComparison.Ordinal)}{CloseQuote}";
    }
}
