using System.Text.RegularExpressions;
using Janatics.DataEngine.FetchService.Abstractions;

namespace Janatics.DataEngine.FetchService.Core;

public class SqlValidator : ISqlValidator
{
    private static readonly string[] ProhibitedKeywords =
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE",
        "MERGE", "REPLACE", "EXEC", "EXECUTE", "CALL", "GRANT", "REVOKE",
        "COMMIT", "ROLLBACK", "SAVEPOINT", "DECLARE", "PROCEDURE", "FUNCTION",
        "TRIGGER", "SCHEMA", "DATABASE", "TABLE", "COLUMN", "SEQUENCE"
    };

    public void ValidateReadOnlyQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("SQL query cannot be null or empty.");

        var cleanSql = NormalizeWhitespace(RemoveComments(sql));

        if (!Regex.IsMatch(cleanSql, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Only SELECT or WITH queries are allowed.");

        if (cleanSql.Contains(';'))
            throw new InvalidOperationException("Multiple statements are not allowed.");

        foreach (var keyword in ProhibitedKeywords)
        {
            if (Regex.IsMatch(cleanSql, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException($"Prohibited SQL keyword detected: {keyword}.");
        }

        var suspiciousPatterns = new[]
        {
            @"--",
            @"/\*",
            @"\bXP_CMDSHELL\b",
            @"\bSP_EXECUTESQL\b",
            @"\bPG_SLEEP\s*\(",
            @"\bWAITFOR\s+DELAY\b"
        };

        foreach (var pattern in suspiciousPatterns)
        {
            if (Regex.IsMatch(cleanSql, pattern, RegexOptions.IgnoreCase))
                throw new InvalidOperationException("Query contains suspicious SQL patterns.");
        }
    }

    public void ValidateDirectQuery(string sql, bool allowDirectQueryExecution, int maxDirectQueryLength)
    {
        if (!allowDirectQueryExecution)
            throw new InvalidOperationException("Direct query execution is not enabled.");

        if (sql.Length > maxDirectQueryLength)
            throw new InvalidOperationException($"Query exceeds maximum allowed length of {maxDirectQueryLength} characters.");

        ValidateReadOnlyQuery(sql);
    }

    private static string RemoveComments(string sql)
    {
        sql = Regex.Replace(sql, @"--.*$", string.Empty, RegexOptions.Multiline);
        sql = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return sql;
    }

    private static string NormalizeWhitespace(string sql)
        => Regex.Replace(sql.Trim(), @"\s+", " ");
}
