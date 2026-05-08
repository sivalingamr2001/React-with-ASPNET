using Janatics.DataEngine.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Janatics.DataEngine.Infrastructure.Repositories;

public sealed class ConfigurationConnectionStringResolver(IConfiguration configuration) : IConnectionStringResolver
{
    public string Resolve(string connectionName)
        => configuration.GetConnectionString(connectionName)
           ?? throw new InvalidOperationException($"Connection string '{connectionName}' was not found.");
}
