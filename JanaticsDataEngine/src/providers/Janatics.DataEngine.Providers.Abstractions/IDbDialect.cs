namespace Janatics.DataEngine.Providers.Abstractions;

public interface IDbDialect
{
    string QuoteIdentifier(string name);
    string ParameterPrefix { get; }
    string PaginationClause(int offset, int limit);
    string ConcurrencyTokenClause(string columnName);
    string LastInsertedIdExpression { get; }
    bool SupportsReturningClause { get; }
}
