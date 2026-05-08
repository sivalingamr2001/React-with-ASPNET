namespace Janatics.DataEngine.Domain.Interfaces;

public interface IQueryValidator
{
    QueryValidationResult Validate(string sql);
}
