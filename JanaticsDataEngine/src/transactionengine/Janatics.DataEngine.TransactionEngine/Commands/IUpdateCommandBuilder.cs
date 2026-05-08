using System.Data;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;

namespace Janatics.DataEngine.TransactionEngine.Commands;

public interface IUpdateCommandBuilder
{
    Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
        object identity,
        IDictionary<string, object?> content,
        IDbProvider provider,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct);
}
