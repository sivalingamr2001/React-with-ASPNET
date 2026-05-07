namespace JanaticsApi.Shared.Abstractions;

public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
}
