namespace JanaticsApi.Shared.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
