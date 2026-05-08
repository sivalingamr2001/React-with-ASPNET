using Janatics.DataEngine.Contracts.Dtos;

namespace Janatics.DataEngine.Contracts.Responses;

public sealed record TransactionResponse
{
    public required Guid TransactionId { get; init; }
    public required bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<NodeMutationResultDto> Results { get; init; } = [];
}
