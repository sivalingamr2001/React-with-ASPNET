using Janatics.DataEngine.Domain.Interfaces;

namespace Janatics.DataEngine.Security.Sanitization;

public sealed class SqlSanitizer(IQueryValidator validator) : ISqlSanitizer
{
    public string Sanitize(string sql)
    {
        var result = validator.Validate(sql);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(result.Error);
        }

        return sql.Trim();
    }
}
