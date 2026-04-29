namespace Janatics.DataEngine.Exceptions;

/// <summary>
/// Thrown when a request cannot be converted into safe parameterised SQL.
/// </summary>
public sealed class SqlBuildException : DataEngineException
{
    public SqlBuildException(string message)
        : base(message, "SQL_BUILD_ERROR")
    {
    }
}
