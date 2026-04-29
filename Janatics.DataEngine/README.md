# Janatics.DataEngine

`Janatics.DataEngine` is a reusable registry-driven class library for dynamic data access. It exposes two services:

- `IFetchService` for provider-aware, paginated reads
- `IForgeService` for transactional create, update, and soft-delete writes

The implementation is SQLite-first for development and test, while the architecture is prepared for SQL Server and Oracle through provider detection, dialect abstraction, and connection factory boundaries.

## Solution layout

- `Janatics.DataEngine` - .NET 10 class library
- `Janatics.DataEngine.Tests` - xUnit test suite
- `Janatics.DataEngine/Scripts/metadata-schema.sql` - metadata registry schema
- `Janatics.DataEngine/Scripts/metadata-seed.sql` - sample registry seed

## Registry model

The metadata registry uses three tables:

- `DbProfiles`
- `EntityRegistry`
- `ColumnRegistry`

These tables drive provider resolution, connection routing, entity discovery, column whitelisting, role gating, search support, and soft-delete behaviour.

## Dependency injection

```csharp
using Janatics.DataEngine.Extensions;

builder.Services.AddJanaticsDataEngine(options =>
{
    options.MetadataProvider = "Sqlite";
    options.MetadataConnectionString = "Data Source=/app/data/meta.db";
    options.CacheTtl = TimeSpan.FromMinutes(5);
    options.MaxPageSize = 1000;
});
```

## Consuming API usage

```csharp
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Services;

public sealed class EmployeeQueryFacade
{
    private readonly IFetchService _fetchService;
    private readonly IForgeService _forgeService;

    public EmployeeQueryFacade(IFetchService fetchService, IForgeService forgeService)
    {
        _fetchService = fetchService;
        _forgeService = forgeService;
    }

    public Task<FetchResult> GetEmployeesAsync(CancellationToken cancellationToken) =>
        _fetchService.FetchAsync(
            new FetchRequest(
                Entity: "Employees",
                Columns: ["EmployeeId", "FirstName", "LastName", "Department"],
                Filters:
                [
                    new FilterCondition("Department", "eq", "Engineering")
                ],
                Sort:
                [
                    new SortOptions("LastName")
                ],
                Page: 1,
                PageSize: 25,
                IncludeCount: true,
                Search: new SearchOptions("an", ["FirstName", "LastName"]),
                Nodes:
                [
                    new NodeRequest(
                        Entity: "EmployeeAddresses",
                        ParentColumn: "EmployeeId",
                        Alias: "Addresses",
                        Columns: ["AddressId", "EmployeeId", "City", "Country"])
                ],
                Roles: ["admin"]),
            cancellationToken);

    public Task<ForgeResult> CreateEmployeeAsync(CancellationToken cancellationToken) =>
        _forgeService.ForgeAsync(
            new ForgeRequest(
                Entity: "Employees",
                Operation: "create",
                Data: new Dictionary<string, object?>
                {
                    ["FirstName"] = "Asha",
                    ["LastName"] = "Raman",
                    ["Department"] = "Engineering",
                    ["Salary"] = 82500m,
                    ["IsDeleted"] = 0
                },
                Nodes:
                [
                    new NodeForge(
                        Entity: "EmployeeAddresses",
                        Operation: "create",
                        ParentColumn: "EmployeeId",
                        Rows:
                        [
                            new Dictionary<string, object?>
                            {
                                ["City"] = "Chennai",
                                ["Country"] = "India",
                                ["IsDeleted"] = 0
                            }
                        ])
                ],
                Roles: ["admin"]),
            cancellationToken);
}
```

## Testing

The test suite uses:

- `xUnit`
- `FluentAssertions`
- `Moq`
- `SQLite in-memory`

It covers SQL generation, registry caching, whitelist validation, transactional forge flows, fetch orchestration, and dialect behaviour.
