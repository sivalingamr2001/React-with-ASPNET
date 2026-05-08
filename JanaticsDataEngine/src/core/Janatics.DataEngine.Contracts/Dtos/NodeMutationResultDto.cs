using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Contracts.Dtos;

public sealed record NodeMutationResultDto(
    string EntityKey,
    OperationType OperationType,
    object? Identity,
    int AffectedRows);
