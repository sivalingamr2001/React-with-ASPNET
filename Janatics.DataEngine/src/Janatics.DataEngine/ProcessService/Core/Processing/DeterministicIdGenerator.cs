using Janatics.DataEngine.Abstractions;

namespace Janatics.DataEngine.Core.Processing;

public sealed class DeterministicIdGenerator : IDeterministicIdGenerator
{
    public Guid CreateId() => Guid.CreateVersion7();
}
