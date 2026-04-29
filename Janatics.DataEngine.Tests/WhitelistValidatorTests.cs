using FluentAssertions;
using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine.Tests;

public sealed class WhitelistValidatorTests
{
    private readonly WhitelistValidator _validator = new(Options.Create(new Janatics.DataEngine.DataEngineOptions { MaxPageSize = 1000 }));
    private readonly Janatics.DataEngine.Registry.EntityMeta _entity = EntityMetaFactory.CreateEmployeesEntity();

    [Fact]
    public void ValidateFetch_should_reject_unknown_select_column()
    {
        var request = new FetchRequest("Employees", Columns: ["Unknown"], Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_unknown_filter_column()
    {
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Unknown", "eq", 1)], Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_unknown_sort_column()
    {
        var request = new FetchRequest("Employees", Sort: [new SortOptions("Unknown")], Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_unknown_search_column()
    {
        var request = new FetchRequest("Employees", Search: new SearchOptions("ash", ["Unknown"]), Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_unknown_operator()
    {
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Department", "regex", "E")], Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_empty_search_term()
    {
        var request = new FetchRequest("Employees", Search: new SearchOptions(" "), Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_page_less_than_one()
    {
        var request = new FetchRequest("Employees", Page: 0, Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_page_size_above_maximum()
    {
        var request = new FetchRequest("Employees", PageSize: 5001, Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_missing_roles_for_secured_entity()
    {
        var request = new FetchRequest("Employees");

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<RoleViolationException>();
    }

    [Fact]
    public void ValidateFetch_should_reject_non_matching_roles()
    {
        var request = new FetchRequest("Employees", Roles: ["guest"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().Throw<RoleViolationException>();
    }

    [Fact]
    public void ValidateFetch_should_accept_matching_roles()
    {
        var request = new FetchRequest("Employees", Roles: ["admin"]);

        var action = () => _validator.ValidateFetch(_entity, request);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateForge_should_reject_unknown_operation()
    {
        var request = new ForgeRequest("Employees", "upsert", new Dictionary<string, object?>(), Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_read_only_entity()
    {
        var entity = EntityMetaFactory.CreateEmployeesEntity(isReadOnly: true);
        var request = new ForgeRequest("Employees", "create", new Dictionary<string, object?> { ["FirstName"] = "A", ["LastName"] = "B" }, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(entity, request);

        action.Should().Throw<ReadOnlyViolationException>();
    }

    [Fact]
    public void ValidateForge_should_reject_missing_data_for_create()
    {
        var request = new ForgeRequest("Employees", "create", null, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_missing_required_column()
    {
        var request = new ForgeRequest("Employees", "create", new Dictionary<string, object?> { ["FirstName"] = "Asha" }, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_missing_key_for_update()
    {
        var request = new ForgeRequest("Employees", "update", new Dictionary<string, object?> { ["Department"] = "Ops" }, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_missing_key_for_delete()
    {
        var request = new ForgeRequest("Employees", "delete", Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_delete_when_soft_delete_column_is_missing()
    {
        var entity = EntityMetaFactory.CreateEmployeesEntity(softDeleteColumn: null);
        var request = new ForgeRequest("Employees", "delete", KeyValue: 1, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_unknown_data_column()
    {
        var request = new ForgeRequest("Employees", "create", new Dictionary<string, object?>
        {
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman",
            ["Unknown"] = "x"
        }, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateForge_should_reject_read_only_column()
    {
        var request = new ForgeRequest("Employees", "create", new Dictionary<string, object?>
        {
            ["EmployeeId"] = 99,
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman"
        }, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().Throw<ReadOnlyViolationException>();
    }

    [Fact]
    public void ValidateNodeFetch_should_reject_missing_parent_column()
    {
        var node = new NodeRequest("EmployeeAddresses", "Unknown");
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeFetch(_entity, addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeFetch_should_reject_invalid_limit()
    {
        var node = new NodeRequest("EmployeeAddresses", "EmployeeId", Limit: 0);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeFetch(_entity, addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeForge_should_reject_missing_rows()
    {
        var node = new NodeForge("EmployeeAddresses", "create", "EmployeeId", []);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeForge(addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeForge_should_reject_missing_parent_column()
    {
        var node = new NodeForge("EmployeeAddresses", "create", "Unknown", [new Dictionary<string, object?> { ["City"] = "Chennai", ["Country"] = "India" }]);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeForge(addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeForge_should_reject_missing_child_key_for_update()
    {
        var node = new NodeForge("EmployeeAddresses", "update", "EmployeeId", [new Dictionary<string, object?> { ["City"] = "Chennai", ["Country"] = "India" }]);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeForge(addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeForge_should_reject_missing_required_column_for_create()
    {
        var node = new NodeForge("EmployeeAddresses", "create", "EmployeeId", [new Dictionary<string, object?> { ["Country"] = "India" }]);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeForge(addressEntity, node, ["admin"]);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void ValidateNodeForge_should_accept_valid_create()
    {
        var node = new NodeForge("EmployeeAddresses", "create", "EmployeeId", [new Dictionary<string, object?> { ["City"] = "Chennai", ["Country"] = "India" }]);
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();

        var action = () => _validator.ValidateNodeForge(addressEntity, node, ["admin"]);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateForge_should_accept_valid_update()
    {
        var request = new ForgeRequest("Employees", "update", new Dictionary<string, object?> { ["Department"] = "Ops" }, 1, Roles: ["admin"]);

        var action = () => _validator.ValidateForge(_entity, request);

        action.Should().NotThrow();
    }
}
