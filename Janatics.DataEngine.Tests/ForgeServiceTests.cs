using FluentAssertions;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Services;
using Janatics.DataEngine.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.Tests;

public sealed class ForgeServiceTests
{
    [Fact]
    public async Task ForgeAsync_should_create_root_and_child_rows_in_one_transaction()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "create",
                new Dictionary<string, object?>
                {
                    ["FirstName"] = "Esha",
                    ["LastName"] = "Varma",
                    ["Department"] = "Engineering",
                    ["Salary"] = 100000,
                    ["IsDeleted"] = 0
                },
                Nodes:
                [
                    new NodeForge(
                        "EmployeeAddresses",
                        "create",
                        "EmployeeId",
                        [
                            new Dictionary<string, object?>
                            {
                                ["City"] = "Pune",
                                ["Country"] = "India",
                                ["IsDeleted"] = 0
                            }
                        ])
                ],
                Roles: ["admin"]));

        result.Success.Should().BeTrue();
        result.RootId.Should().NotBeNull();
        var employeeCount = await harness.CountAsync("SELECT COUNT(1) FROM Employees WHERE FirstName = 'Esha'");
        var childCount = await harness.CountAsync("SELECT COUNT(1) FROM EmployeeAddresses WHERE City = 'Pune'");
        employeeCount.Should().Be(1);
        childCount.Should().Be(1);
    }

    [Fact]
    public async Task ForgeAsync_should_update_existing_row()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "update",
                new Dictionary<string, object?>
                {
                    ["Department"] = "Strategy",
                    ["Salary"] = 99000
                },
                2,
                Roles: ["admin"]));

        result.Success.Should().BeTrue();
        var department = await harness.ScalarAsync<string>("SELECT Department FROM Employees WHERE EmployeeId = 2");
        department.Should().Be("Strategy");
    }

    [Fact]
    public async Task ForgeAsync_should_soft_delete_existing_row()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(new ForgeRequest("Employees", "delete", KeyValue: 2, Roles: ["admin"]));

        result.Success.Should().BeTrue();
        var deleted = await harness.ScalarAsync<long>("SELECT IsDeleted FROM Employees WHERE EmployeeId = 2");
        deleted.Should().Be(1);
    }

    [Fact]
    public async Task ForgeAsync_should_update_child_rows()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "update",
                new Dictionary<string, object?>
                {
                    ["Department"] = "Operations"
                },
                1,
                Nodes:
                [
                    new NodeForge(
                        "EmployeeAddresses",
                        "update",
                        "EmployeeId",
                        [
                            new Dictionary<string, object?>
                            {
                                ["AddressId"] = 1,
                                ["City"] = "Madurai",
                                ["Country"] = "India"
                            }
                        ])
                ],
                Roles: ["admin"]));

        result.Success.Should().BeTrue();
        var city = await harness.ScalarAsync<string>("SELECT City FROM EmployeeAddresses WHERE AddressId = 1");
        city.Should().Be("Madurai");
    }

    [Fact]
    public async Task ForgeAsync_should_soft_delete_child_rows()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "update",
                new Dictionary<string, object?>
                {
                    ["Department"] = "Operations"
                },
                1,
                Nodes:
                [
                    new NodeForge(
                        "EmployeeAddresses",
                        "delete",
                        "EmployeeId",
                        [
                            new Dictionary<string, object?>
                            {
                                ["AddressId"] = 2
                            }
                        ])
                ],
                Roles: ["admin"]));

        result.Success.Should().BeTrue();
        var deleted = await harness.ScalarAsync<long>("SELECT IsDeleted FROM EmployeeAddresses WHERE AddressId = 2");
        deleted.Should().Be(1);
    }

    [Fact]
    public async Task ForgeAsync_should_rollback_when_child_insert_fails()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var action = async () => await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "create",
                new Dictionary<string, object?>
                {
                    ["FirstName"] = "Failing",
                    ["LastName"] = "User",
                    ["Department"] = "Engineering",
                    ["Salary"] = 100000,
                    ["IsDeleted"] = 0
                },
                Nodes:
                [
                    new NodeForge(
                        "EmployeeAddresses",
                        "create",
                        "EmployeeId",
                        [
                            new Dictionary<string, object?>
                            {
                                ["Country"] = "India"
                            }
                        ])
                ],
                Roles: ["admin"]));

        await action.Should().ThrowAsync<SqlBuildException>();
        var employeeCount = await harness.CountAsync("SELECT COUNT(1) FROM Employees WHERE FirstName = 'Failing'");
        employeeCount.Should().Be(0);
    }

    [Fact]
    public async Task ForgeAsync_should_return_generated_identity_for_create()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var result = await service.ForgeAsync(
            new ForgeRequest(
                "Employees",
                "create",
                new Dictionary<string, object?>
                {
                    ["FirstName"] = "Hari",
                    ["LastName"] = "Prasad",
                    ["Department"] = "Finance",
                    ["Salary"] = 55000,
                    ["IsDeleted"] = 0
                },
                Roles: ["admin"]));

        result.RootId.Should().BeOfType<long>();
    }

    [Fact]
    public async Task ForgeAsync_should_throw_for_unknown_entity()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IForgeService>();

        var action = async () => await service.ForgeAsync(new ForgeRequest("Unknown", "create", new Dictionary<string, object?>(), Roles: ["admin"]));

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }
}
