using Dapper;
using Janatics.DataEngine.Extensions;
using Janatics.DataEngine.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janatics.DataEngine.Tests.TestSupport;

internal sealed class SqliteTestHarness : IAsyncDisposable
{
    private readonly SqliteConnection _metadataKeeper;
    private readonly SqliteConnection _dataKeeper;

    private SqliteTestHarness(SqliteConnection metadataKeeper, SqliteConnection dataKeeper)
    {
        _metadataKeeper = metadataKeeper;
        _dataKeeper = dataKeeper;
    }

    public string MetadataConnectionString => _metadataKeeper.ConnectionString;

    public string DataConnectionString => _dataKeeper.ConnectionString;

    public static async Task<SqliteTestHarness> CreateAsync()
    {
        var metadataBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = $"meta-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        var dataBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = $"data-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        var metadataKeeper = new SqliteConnection(metadataBuilder.ToString());
        var dataKeeper = new SqliteConnection(dataBuilder.ToString());
        await metadataKeeper.OpenAsync();
        await dataKeeper.OpenAsync();

        var harness = new SqliteTestHarness(metadataKeeper, dataKeeper);
        await harness.InitializeMetadataAsync();
        await harness.InitializeDataAsync();
        return harness;
    }

    public ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddJanaticsDataEngine(options =>
        {
            options.MetadataProvider = "Sqlite";
            options.MetadataConnectionString = MetadataConnectionString;
            options.CacheTtl = TimeSpan.FromMinutes(5);
            options.MaxPageSize = 1000;
        });

        return services.BuildServiceProvider();
    }

    public async Task<IFetchService> CreateFetchServiceAsync()
    {
        var provider = CreateServiceProvider();
        await Task.CompletedTask;
        return provider.GetRequiredService<IFetchService>();
    }

    public async Task<IForgeService> CreateForgeServiceAsync()
    {
        var provider = CreateServiceProvider();
        await Task.CompletedTask;
        return provider.GetRequiredService<IForgeService>();
    }

    public async Task<int> CountAsync(string sql, object? parameters = null)
    {
        await using var connection = new SqliteConnection(DataConnectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<T> ScalarAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = new SqliteConnection(DataConnectionString);
        await connection.OpenAsync();
        return (await connection.ExecuteScalarAsync<T>(sql, parameters))!;
    }

    public async Task<IReadOnlyList<dynamic>> QueryAsync(string sql, object? parameters = null)
    {
        await using var connection = new SqliteConnection(DataConnectionString);
        await connection.OpenAsync();
        return (await connection.QueryAsync(sql, parameters)).Cast<dynamic>().ToList();
    }

    public async ValueTask DisposeAsync()
    {
        await _metadataKeeper.DisposeAsync();
        await _dataKeeper.DisposeAsync();
    }

    private async Task InitializeMetadataAsync()
    {
        const string metadataSql = """
            CREATE TABLE DbProfiles (
                ProfileId TEXT NOT NULL PRIMARY KEY,
                ProfileName TEXT NOT NULL,
                Provider TEXT NOT NULL,
                ConnectionString TEXT NOT NULL,
                Status TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE EntityRegistry (
                EntityId TEXT NOT NULL PRIMARY KEY,
                ProfileId TEXT NOT NULL,
                EntityName TEXT NOT NULL,
                TableName TEXT NOT NULL,
                SchemaName TEXT NULL,
                PkColumn TEXT NOT NULL,
                SoftDeleteColumn TEXT NULL,
                IsReadOnly INTEGER NOT NULL,
                AllowedRoles TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE ColumnRegistry (
                ColumnId TEXT NOT NULL PRIMARY KEY,
                EntityId TEXT NOT NULL,
                ColumnName TEXT NOT NULL,
                DataType TEXT NOT NULL,
                IsRequired INTEGER NOT NULL,
                IsPrimaryKey INTEGER NOT NULL,
                IsSearchable INTEGER NOT NULL,
                IsReadOnly INTEGER NOT NULL,
                IsNullable INTEGER NOT NULL,
                OrdinalPosition INTEGER NOT NULL
            );
            """;

        await _metadataKeeper.ExecuteAsync(metadataSql);

        await _metadataKeeper.ExecuteAsync(
            """
            INSERT INTO DbProfiles (ProfileId, ProfileName, Provider, ConnectionString, Status, CreatedAt, UpdatedAt)
            VALUES (@ProfileId, 'DevSqlite', 'Sqlite', @ConnectionString, 'active', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """,
            new
            {
                ProfileId = "11111111-1111-1111-1111-111111111111",
                ConnectionString = DataConnectionString
            });

        await _metadataKeeper.ExecuteAsync(
            """
            INSERT INTO EntityRegistry (EntityId, ProfileId, EntityName, TableName, SchemaName, PkColumn, SoftDeleteColumn, IsReadOnly, AllowedRoles, CreatedAt, UpdatedAt)
            VALUES
            ('22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', 'Employees', 'Employees', NULL, 'EmployeeId', 'IsDeleted', 0, 'admin,reporting', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
            ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'EmployeeAddresses', 'EmployeeAddresses', NULL, 'AddressId', 'IsDeleted', 0, 'admin,reporting', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """);

        await _metadataKeeper.ExecuteAsync(
            """
            INSERT INTO ColumnRegistry (ColumnId, EntityId, ColumnName, DataType, IsRequired, IsPrimaryKey, IsSearchable, IsReadOnly, IsNullable, OrdinalPosition)
            VALUES
            ('33333333-3333-3333-3333-333333333331', '22222222-2222-2222-2222-222222222221', 'EmployeeId', 'INTEGER', 0, 1, 0, 1, 0, 1),
            ('33333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222221', 'FirstName', 'TEXT', 1, 0, 1, 0, 0, 2),
            ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222221', 'LastName', 'TEXT', 1, 0, 1, 0, 0, 3),
            ('33333333-3333-3333-3333-333333333334', '22222222-2222-2222-2222-222222222221', 'Department', 'TEXT', 0, 0, 1, 0, 1, 4),
            ('33333333-3333-3333-3333-333333333335', '22222222-2222-2222-2222-222222222221', 'Salary', 'REAL', 0, 0, 0, 0, 1, 5),
            ('33333333-3333-3333-3333-333333333336', '22222222-2222-2222-2222-222222222221', 'IsDeleted', 'INTEGER', 0, 0, 0, 0, 0, 6),
            ('33333333-3333-3333-3333-333333333337', '22222222-2222-2222-2222-222222222222', 'AddressId', 'INTEGER', 0, 1, 0, 1, 0, 1),
            ('33333333-3333-3333-3333-333333333338', '22222222-2222-2222-2222-222222222222', 'EmployeeId', 'INTEGER', 1, 0, 0, 0, 0, 2),
            ('33333333-3333-3333-3333-333333333339', '22222222-2222-2222-2222-222222222222', 'City', 'TEXT', 1, 0, 1, 0, 0, 3),
            ('33333333-3333-3333-3333-333333333340', '22222222-2222-2222-2222-222222222222', 'Country', 'TEXT', 1, 0, 1, 0, 0, 4),
            ('33333333-3333-3333-3333-333333333341', '22222222-2222-2222-2222-222222222222', 'IsDeleted', 'INTEGER', 0, 0, 0, 0, 0, 5);
            """);
    }

    private async Task InitializeDataAsync()
    {
        const string dataSql = """
            CREATE TABLE Employees (
                EmployeeId INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Department TEXT NULL,
                Salary REAL NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE EmployeeAddresses (
                AddressId INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                City TEXT NOT NULL,
                Country TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            """;

        await _dataKeeper.ExecuteAsync(dataSql);

        await _dataKeeper.ExecuteAsync(
            """
            INSERT INTO Employees (EmployeeId, FirstName, LastName, Department, Salary, IsDeleted)
            VALUES
            (1, 'Asha', 'Raman', 'Engineering', 85000, 0),
            (2, 'Bala', 'Krish', 'Operations', 72000, 0),
            (3, 'Chitra', 'Nair', 'Engineering', 91000, 1),
            (4, 'Deepa', 'Iyer', 'Finance', 68000, 0);
            """);

        await _dataKeeper.ExecuteAsync(
            """
            INSERT INTO EmployeeAddresses (AddressId, EmployeeId, City, Country, IsDeleted)
            VALUES
            (1, 1, 'Chennai', 'India', 0),
            (2, 1, 'Bengaluru', 'India', 0),
            (3, 2, 'Coimbatore', 'India', 0),
            (4, 3, 'Hyderabad', 'India', 0);
            """);
    }
}
