namespace Janatics.DataEngine.Domain.Interfaces;

public sealed record QueryValidationResult(bool IsValid, string? Error)
{
    public static QueryValidationResult Success() => new(true, null);
    public static QueryValidationResult Failure(string error) => new(false, error);
}
