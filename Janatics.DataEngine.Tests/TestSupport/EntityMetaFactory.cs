using Janatics.DataEngine.Registry;

namespace Janatics.DataEngine.Tests.TestSupport;

internal static class EntityMetaFactory
{
    public static EntityMeta CreateEmployeesEntity(
        bool isReadOnly = false,
        string? softDeleteColumn = "IsDeleted",
        IReadOnlySet<string>? roles = null)
    {
        var entityId = Guid.Parse("22222222-2222-2222-2222-222222222221");
        var profileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var columns = new[]
        {
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333331"), entityId, "EmployeeId", "INTEGER", false, true, false, true, false, 1),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333332"), entityId, "FirstName", "TEXT", true, false, true, false, false, 2),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333333"), entityId, "LastName", "TEXT", true, false, true, false, false, 3),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333334"), entityId, "Department", "TEXT", false, false, true, false, true, 4),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333335"), entityId, "Salary", "REAL", false, false, false, false, true, 5),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333336"), entityId, "IsDeleted", "INTEGER", false, false, false, false, false, 6)
        };

        return CreateEntity(
            entityId,
            profileId,
            "Employees",
            "Employees",
            "EmployeeId",
            softDeleteColumn,
            isReadOnly,
            roles ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "admin", "reporting" },
            columns);
    }

    public static EntityMeta CreateAddressesEntity(bool isReadOnly = false)
    {
        var entityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var profileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var columns = new[]
        {
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333337"), entityId, "AddressId", "INTEGER", false, true, false, true, false, 1),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333338"), entityId, "EmployeeId", "INTEGER", true, false, false, false, false, 2),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333339"), entityId, "City", "TEXT", true, false, true, false, false, 3),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333340"), entityId, "Country", "TEXT", true, false, true, false, false, 4),
            new ColumnMeta(Guid.Parse("33333333-3333-3333-3333-333333333341"), entityId, "IsDeleted", "INTEGER", false, false, false, false, false, 5)
        };

        return CreateEntity(
            entityId,
            profileId,
            "EmployeeAddresses",
            "EmployeeAddresses",
            "AddressId",
            "IsDeleted",
            isReadOnly,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "admin", "reporting" },
            columns);
    }

    public static DbProfile CreateProfile(string? connectionString = null) =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "DevSqlite",
            "Sqlite",
            connectionString ?? "Data Source=:memory:",
            "active");

    private static EntityMeta CreateEntity(
        Guid entityId,
        Guid profileId,
        string entityName,
        string tableName,
        string primaryKey,
        string? softDeleteColumn,
        bool isReadOnly,
        IReadOnlySet<string> roles,
        IReadOnlyList<ColumnMeta> columns)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        return new EntityMeta(
            entityId,
            profileId,
            entityName,
            tableName,
            null,
            primaryKey,
            softDeleteColumn,
            isReadOnly,
            roles,
            columns.ToDictionary(column => column.ColumnName, comparer),
            new HashSet<string>(columns.Where(column => column.IsRequired).Select(column => column.ColumnName), comparer),
            new HashSet<string>(columns.Where(column => column.IsSearchable).Select(column => column.ColumnName), comparer));
    }
}
