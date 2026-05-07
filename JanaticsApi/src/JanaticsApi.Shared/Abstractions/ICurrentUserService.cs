namespace JanaticsApi.Shared.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}
