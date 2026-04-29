using Dapper;
using FluentAssertions;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Registry;
using Janatics.DataEngine.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.Tests;

public sealed class RegistryServiceTests
{
    [Fact]
    public async Task GetEntityAsync_should_load_entity_metadata_from_registry()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var entity = await registry.GetEntityAsync("Employees");

        entity.EntityName.Should().Be("Employees");
        entity.PrimaryKey.Should().Be("EmployeeId");
        entity.Columns.Should().ContainKey("FirstName");
    }

    [Fact]
    public async Task GetProfileAsync_should_load_profile_from_registry()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var profile = await registry.GetProfileAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        profile.ProfileName.Should().Be("DevSqlite");
        profile.Provider.Should().Be("Sqlite");
    }

    [Fact]
    public async Task GetEntityAsync_should_throw_for_unknown_entity()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var action = async () => await registry.GetEntityAsync("Missing");

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetEntityAsync_should_return_cached_value_when_registry_rows_are_removed_after_first_read()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var first = await registry.GetEntityAsync("Employees");

        await using (var metadata = new SqliteConnection(harness.MetadataConnectionString))
        {
            await metadata.OpenAsync();
            await metadata.ExecuteAsync("DELETE FROM ColumnRegistry; DELETE FROM EntityRegistry;");
        }

        var second = await registry.GetEntityAsync("Employees");

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task ClearCacheAsync_should_force_registry_reload()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        _ = await registry.GetEntityAsync("Employees");

        await using (var metadata = new SqliteConnection(harness.MetadataConnectionString))
        {
            await metadata.OpenAsync();
            await metadata.ExecuteAsync("DELETE FROM ColumnRegistry; DELETE FROM EntityRegistry;");
        }

        await registry.ClearCacheAsync();

        var action = async () => await registry.GetEntityAsync("Employees");

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetEntityAsync_should_build_required_and_searchable_sets()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var entity = await registry.GetEntityAsync("Employees");

        entity.RequiredColumns.Should().Contain(["FirstName", "LastName"]);
        entity.SearchableColumns.Should().Contain(["FirstName", "LastName", "Department"]);
    }

    [Fact]
    public async Task GetEntityAsync_should_parse_allowed_roles()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();

        var entity = await registry.GetEntityAsync("Employees");

        entity.AllowedRoles.Should().Contain(["admin", "reporting"]);
    }

    [Fact]
    public async Task GetProfileAsync_should_return_cached_profile_after_registry_rows_are_removed()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var registry = provider.GetRequiredService<IEntityRegistryService>();
        var profileId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = await registry.GetProfileAsync(profileId);

        await using (var metadata = new SqliteConnection(harness.MetadataConnectionString))
        {
            await metadata.OpenAsync();
            await metadata.ExecuteAsync("DELETE FROM DbProfiles;");
        }

        var second = await registry.GetProfileAsync(profileId);

        second.Should().BeEquivalentTo(first);
    }
}
