using DataEngine.Infrastruture.Models;
using System.Data;
using System.Threading.Tasks;

namespace DataEngine.Infrastruture.ConnectionFactory
{
    public interface IResilientConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config);
        Task<IDbConnection> CreateReadOnlyConnectionAsync(DatabaseConfig config, ReadReplicaConfig? replicaConfig = null);
        Task<bool> ValidateConnectionHealthAsync(IDbConnection connection);
        ConnectionPoolMetrics GetPoolMetrics();
        Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config);
        Task<ConnectionPoolHealth> GetPoolHealthAsync();
        Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics);
    }
}
