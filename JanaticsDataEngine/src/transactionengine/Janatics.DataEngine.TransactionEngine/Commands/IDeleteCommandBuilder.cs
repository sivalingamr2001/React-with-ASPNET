using System.Data;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;

namespace Janatics.DataEngine.TransactionEngine.Commands;

public interface IDeleteCommandBuilder
{
    Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
        object identity,
        IDbProvider provider,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct);
}
