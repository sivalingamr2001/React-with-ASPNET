using Janatics.DataEngine.ProcessService.Abstractions;

namespace Janatics.DataEngine.ProcessService.Core.Processing;

public sealed class DeterministicIdGenerator : IDeterministicIdGenerator
{
    public Guid CreateId() => Guid.CreateVersion7();
}
