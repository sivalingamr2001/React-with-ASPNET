namespace Janatics.DataEngine.FetchService.Abstractions;

public interface ISqlValidator
{
    void ValidateReadOnlyQuery(string sql);
    void ValidateDirectQuery(string sql, bool allowDirectQueryExecution, int maxDirectQueryLength);
}
