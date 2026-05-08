using Janatics.DataEngine.Domain.Interfaces;

namespace Janatics.DataEngine.Security.Sanitization;

public sealed class AllowedOperationValidator : IQueryValidator
{
    private static readonly string[] AllowedStartKeywords = ["SELECT", "WITH"];
    private static readonly string[] DeniedKeywords =
    [
        "DROP",
        "TRUNCATE",
        "DELETE",
        "UPDATE",
        "INSERT",
        "ALTER",
        "CREATE",
        "GRANT",
        "REVOKE",
        "EXEC",
        "EXECUTE",
        "SP_",
        "XP_",
        "--",
        "/*",
        "*/"
    ];

    public QueryValidationResult Validate(string sql)
    {
        var normalizedSql = sql.Trim().ToUpperInvariant();

        if (!AllowedStartKeywords.Any(x => normalizedSql.StartsWith(x, StringComparison.Ordinal)))
        {
            return QueryValidationResult.Failure("Query must start with SELECT or WITH.");
        }

        foreach (var deniedKeyword in DeniedKeywords)
        {
            if (normalizedSql.Contains(deniedKeyword, StringComparison.Ordinal))
            {
                return QueryValidationResult.Failure($"Disallowed keyword detected: {deniedKeyword}");
            }
        }

        return QueryValidationResult.Success();
    }
}
