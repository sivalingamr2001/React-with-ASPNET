namespace Janatics.DataEngine.ProcessService.Abstractions;

public interface IDeterministicIdGenerator
{
    Guid CreateId();
}
