using FluentAssertions;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Services;
using Janatics.DataEngine.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.Tests;

public sealed class FetchServiceTests
{
    [Fact]
    public async Task FetchAsync_should_return_paged_rows_and_total_count()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(new FetchRequest("Employees", Page: 1, PageSize: 2, IncludeCount: true, Roles: ["admin"]));

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task FetchAsync_should_apply_filters()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(
            new FetchRequest("Employees", Filters: [new FilterCondition("Department", "eq", "Engineering")], Roles: ["admin"]));

        result.Data.Should().ContainSingle();
        result.Data[0]["FirstName"].Should().Be("Asha");
    }

    [Fact]
    public async Task FetchAsync_should_attach_child_nodes()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(
            new FetchRequest(
                "Employees",
                Columns: ["EmployeeId", "FirstName"],
                Filters: [new FilterCondition("EmployeeId", "eq", 1)],
                Nodes: [new NodeRequest("EmployeeAddresses", "EmployeeId", "Addresses")],
                Roles: ["admin"]));

        result.Data.Should().ContainSingle();
        var children = result.Data[0]["Addresses"].Should().BeAssignableTo<IReadOnlyList<IReadOnlyDictionary<string, object?>>>().Subject;
        children.Should().HaveCount(2);
    }

    [Fact]
    public async Task FetchAsync_should_apply_search_terms()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(
            new FetchRequest("Employees", Search: new SearchOptions("dee"), Roles: ["admin"]));

        result.Data.Should().ContainSingle();
        result.Data[0]["FirstName"].Should().Be("Deepa");
    }

    [Fact]
    public async Task FetchAsync_should_exclude_soft_deleted_rows_by_default()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(new FetchRequest("Employees", IncludeCount: true, Roles: ["admin"]));

        result.TotalCount.Should().Be(3);
        result.Data.Should().OnlyContain(row => Convert.ToInt32(row["IsDeleted"]) == 0);
    }

    [Fact]
    public async Task FetchAsync_should_include_soft_deleted_rows_when_requested()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(new FetchRequest("Employees", IncludeCount: true, IncludeSoftDeleted: true, Roles: ["admin"]));

        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task FetchAsync_should_apply_sorting_and_paging()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var result = await service.FetchAsync(
            new FetchRequest("Employees", Sort: [new SortOptions("FirstName")], Page: 2, PageSize: 1, Roles: ["admin"]));

        result.Data.Should().ContainSingle();
        result.Data[0]["FirstName"].Should().Be("Bala");
    }

    [Fact]
    public async Task FetchAsync_should_throw_for_unknown_entity()
    {
        await using var harness = await SqliteTestHarness.CreateAsync();
        using var provider = harness.CreateServiceProvider();
        var service = provider.GetRequiredService<IFetchService>();

        var action = async () => await service.FetchAsync(new FetchRequest("Missing", Roles: ["admin"]));

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }
}
