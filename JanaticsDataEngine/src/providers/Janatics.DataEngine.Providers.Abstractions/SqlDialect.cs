namespace Janatics.DataEngine.Providers.Abstractions;

public sealed class SqlDialect : IDbDialect
{
    private readonly string _openQuote;
    private readonly string _closeQuote;

    public SqlDialect(string parameterPrefix, string openQuote = "[", string closeQuote = "]", string lastInsertedIdExpression = "SELECT SCOPE_IDENTITY();")
    {
        ParameterPrefix = parameterPrefix;
        _openQuote = openQuote;
        _closeQuote = closeQuote;
        LastInsertedIdExpression = lastInsertedIdExpression;
    }

    public string ParameterPrefix { get; }
    public string LastInsertedIdExpression { get; }
    public bool SupportsReturningClause => false;

    public string QuoteIdentifier(string name) => $"{_openQuote}{name}{_closeQuote}";

    public string PaginationClause(int offset, int limit) => $" OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

    public string ConcurrencyTokenClause(string columnName) => $"{QuoteIdentifier(columnName)} = {ParameterPrefix}{columnName}";
}
