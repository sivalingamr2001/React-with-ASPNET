using JanaticsApi.Shared.Abstractions;

namespace JanaticsApi.API.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
