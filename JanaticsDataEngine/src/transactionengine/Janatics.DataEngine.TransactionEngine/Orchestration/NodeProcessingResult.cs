using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public sealed record NodeProcessingResult(
    string EntityKey,
    OperationType OperationType,
    object? Identity,
    int AffectedRows);
