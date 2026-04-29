namespace Janatics.DataEngine.Exceptions;

/// <summary>
/// Thrown when a requested entity is missing from the registry.
/// </summary>
public sealed class EntityNotFoundException : DataEngineException
{
    public EntityNotFoundException(string? entityName)
        : base($"Entity '{entityName}' is not registered in the DataEngine registry.", "ENTITY_NOT_FOUND")
    {
    }
}
