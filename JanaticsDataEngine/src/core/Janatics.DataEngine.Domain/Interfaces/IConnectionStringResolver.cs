namespace Janatics.DataEngine.Domain.Interfaces;

public interface IConnectionStringResolver
{
    string Resolve(string connectionName);
}
