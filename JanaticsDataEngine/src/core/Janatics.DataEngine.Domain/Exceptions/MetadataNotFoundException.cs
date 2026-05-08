namespace Janatics.DataEngine.Domain.Exceptions;

public sealed class MetadataNotFoundException : DataEngineException
{
    public MetadataNotFoundException(string key)
        : base($"Metadata not found for key '{key}'.")
    {
    }
}
