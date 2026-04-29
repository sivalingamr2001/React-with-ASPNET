namespace Janatics.DataEngine.Exceptions;

/// <summary>
/// Thrown when a caller lacks a required registry role.
/// </summary>
public sealed class RoleViolationException : DataEngineException
{
    public RoleViolationException(string entityName)
        : base($"The caller is not authorised to access entity '{entityName}'.", "ROLE_VIOLATION")
    {
    }
}
