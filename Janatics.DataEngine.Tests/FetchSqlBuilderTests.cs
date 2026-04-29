using Dapper;
using FluentAssertions;
using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Tests.TestSupport;

namespace Janatics.DataEngine.Tests;

public sealed class FetchSqlBuilderTests
{
    private static readonly Janatics.DataEngine.Registry.EntityMeta Entity = EntityMetaFactory.CreateEmployeesEntity();

    [Theory]
    [InlineData("eq", "=")]
    [InlineData("neq", "<>")]
    [InlineData("gt", ">")]
    [InlineData("gte", ">=")]
    [InlineData("lt", "<")]
    [InlineData("lte", "<=")]
    public void Build_should_render_binary_filter_operators(string op, string expectedOperator)
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Salary", op, 5000)]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain($"\"Salary\" {expectedOperator} @p0");
        statement.Parameters.Get<object>("p0").Should().Be(5000);
    }

    [Theory]
    [InlineData("contains", "%ash%")]
    [InlineData("startsWith", "ash%")]
    [InlineData("endsWith", "%ash")]
    public void Build_should_render_like_operators(string op, string expectedValue)
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("FirstName", op, "ash")]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("\"FirstName\" LIKE @p0 ESCAPE '\\'");
        statement.Parameters.Get<string>("p0").Should().Be(expectedValue);
    }

    [Fact]
    public void Build_should_render_between_filter()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Salary", "between", 1000, 2000)]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("\"Salary\" BETWEEN @p0_start AND @p0_end");
        statement.Parameters.Get<object>("p0_start").Should().Be(1000);
        statement.Parameters.Get<object>("p0_end").Should().Be(2000);
    }

    [Fact]
    public void Build_should_render_in_filter()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Department", "in", Values: ["Engineering", "Finance"])]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("\"Department\" IN @p0");
    }

    [Theory]
    [InlineData("isNull", "IS NULL")]
    [InlineData("isNotNull", "IS NOT NULL")]
    public void Build_should_render_null_operators(string op, string expected)
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Department", op)]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain($"\"Department\" {expected}");
    }

    [Fact]
    public void Build_should_select_all_registered_columns_when_not_specified()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees"));

        statement.Sql.Should().StartWith("SELECT \"EmployeeId\", \"FirstName\", \"LastName\", \"Department\", \"Salary\", \"IsDeleted\"");
    }

    [Fact]
    public void Build_should_select_only_requested_columns()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees", Columns: ["EmployeeId", "FirstName"]));

        statement.Sql.Should().StartWith("SELECT \"EmployeeId\", \"FirstName\"");
        statement.Sql.Should().NotContain("\"LastName\"");
    }

    [Fact]
    public void Build_should_include_count_sql_when_requested()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees", IncludeCount: true));

        statement.CountSql.Should().Be("SELECT COUNT(1) FROM \"Employees\" WHERE \"IsDeleted\" = 0");
    }

    [Theory]
    [InlineData(1, 10, "@limit", "@offset", 10, 0)]
    [InlineData(3, 25, "@limit", "@offset", 25, 50)]
    public void Build_should_generate_paging_parameters(int page, int pageSize, string limitParam, string offsetParam, int expectedLimit, int expectedOffset)
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees", Page: page, PageSize: pageSize));

        statement.Sql.Should().Contain($"LIMIT {limitParam} OFFSET {offsetParam}");
        statement.Parameters.Get<int>("limit").Should().Be(expectedLimit);
        statement.Parameters.Get<int>("offset").Should().Be(expectedOffset);
    }

    [Fact]
    public void Build_should_clamp_page_size_to_maximum()
    {
        var builder = CreateBuilder(maxPageSize: 100);

        var statement = builder.Build(Entity, new FetchRequest("Employees", PageSize: 5000));

        statement.Parameters.Get<int>("limit").Should().Be(100);
    }

    [Fact]
    public void Build_should_default_invalid_page_to_first_page()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees", Page: 0));

        statement.Parameters.Get<int>("offset").Should().Be(0);
    }

    [Fact]
    public void Build_should_apply_explicit_sorting()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Sort: [new SortOptions("LastName"), new SortOptions("FirstName", true)]);

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("ORDER BY \"LastName\" ASC, \"FirstName\" DESC");
    }

    [Fact]
    public void Build_should_default_sort_to_primary_key()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees"));

        statement.Sql.Should().Contain("ORDER BY \"EmployeeId\" ASC");
    }

    [Fact]
    public void Build_should_filter_out_soft_deleted_rows_by_default()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees"));

        statement.Sql.Should().Contain("WHERE \"IsDeleted\" = 0");
    }

    [Fact]
    public void Build_should_skip_soft_delete_filter_when_requested()
    {
        var builder = CreateBuilder();

        var statement = builder.Build(Entity, new FetchRequest("Employees", IncludeSoftDeleted: true));

        statement.Sql.Should().NotContain("\"IsDeleted\" = 0");
    }

    [Fact]
    public void Build_should_add_search_clause_using_request_columns()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Search: new SearchOptions("ash", ["FirstName"]));

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("(\"FirstName\" LIKE @search_0 ESCAPE '\\')");
        statement.Parameters.Get<string>("search_0").Should().Be("%ash%");
    }

    [Fact]
    public void Build_should_add_search_clause_using_registry_searchable_columns()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Search: new SearchOptions("eng"));

        var statement = builder.Build(Entity, request);

        statement.Sql.Should().Contain("\"FirstName\" LIKE @search_0 ESCAPE '\\'");
        statement.Sql.Should().Contain("\"LastName\" LIKE @search_1 ESCAPE '\\'");
        statement.Sql.Should().Contain("\"Department\" LIKE @search_2 ESCAPE '\\'");
    }

    [Fact]
    public void Build_should_escape_like_special_characters()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Search: new SearchOptions("50%_off"));

        var statement = builder.Build(Entity, request);

        statement.Parameters.Get<string>("search_0").Should().Be("%50\\%\\_off%");
    }

    [Fact]
    public void BuildNode_should_generate_parent_key_filter_and_limit()
    {
        var builder = CreateBuilder();
        var addressEntity = EntityMetaFactory.CreateAddressesEntity();
        var request = new NodeRequest("EmployeeAddresses", "EmployeeId", Limit: 5);

        var statement = builder.BuildNode(addressEntity, request, [1, 2]);

        statement.Sql.Should().Contain("\"EmployeeId\" IN @parentKeys");
        statement.Sql.Should().Contain("LIMIT @nodeLimit");
        statement.Parameters.Get<int>("nodeLimit").Should().Be(5);
    }

    [Fact]
    public void Build_should_throw_for_invalid_in_filter_without_values()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Department", "in")]);

        var action = () => builder.Build(Entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void Build_should_throw_for_unknown_operator()
    {
        var builder = CreateBuilder();
        var request = new FetchRequest("Employees", Filters: [new FilterCondition("Department", "regex", "E")]);

        var action = () => builder.Build(Entity, request);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void Build_should_work_without_soft_delete_column()
    {
        var builder = CreateBuilder();
        var entity = EntityMetaFactory.CreateEmployeesEntity(softDeleteColumn: null);

        var statement = builder.Build(entity, new FetchRequest("Employees"));

        statement.Sql.Should().NotContain("WHERE \"IsDeleted\" = 0");
    }

    [Fact]
    public void Build_should_quote_schema_qualified_tables()
    {
        var builder = CreateBuilder();
        var entity = Entity with { SchemaName = "main" };

        var statement = builder.Build(entity, new FetchRequest("Employees"));

        statement.Sql.Should().Contain("FROM \"main\".\"Employees\"");
    }

    private static FetchSqlBuilder CreateBuilder(int maxPageSize = 1000) =>
        new(SqlDialect.ForProvider("Sqlite"), maxPageSize);
}
