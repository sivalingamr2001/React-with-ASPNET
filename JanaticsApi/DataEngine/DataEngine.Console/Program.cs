using DataEngine.Infrastruture.ConnectionFactory;
using DataEngine.Infrastruture.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

namespace DataEngine.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {

        var config = new DatabaseConfig
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = "Data Source=DataEngineDev.db;Cache=Shared",
            EnablePooling = true,
            MaxPoolSize = 50,
            MinPoolSize = 0,
            ConnectionTimeoutSeconds = 15,
            CommandTimeoutSeconds = 30,
            ConnectionIdleLifetimeMinutes = 5
        };

        var logger = NullLogger<ResilientConnectionFactory>.Instance;
        var factory = new ResilientConnectionFactory(logger);

        Console.WriteLine("Starting DataEngine dev test using SQLite...");

        using var connection = await factory.CreateConnectionAsync(config);
        Console.WriteLine($"Connection opened: {connection.State}");
        Console.WriteLine($"Database provider: {config.Provider}");
        Console.WriteLine($"Connection string: {config.ConnectionString}");

        if (connection.State == ConnectionState.Open)
        {
            Console.WriteLine("SQLite connection is ready for dev testing.");
        }
        else
        {
            Console.WriteLine("Failed to open the SQLite connection.");
        }
    }
}