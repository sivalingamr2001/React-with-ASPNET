using Dapper;
using FluentAssertions;
using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Tests.TestSupport;

namespace Janatics.DataEngine.Tests;

public sealed class ForgeSqlBuilderTests
{
    private static readonly Janatics.DataEngine.Registry.EntityMeta Entity = EntityMetaFactory.CreateEmployeesEntity();

    [Fact]
    public void BuildCreate_should_generate_insert_sql()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman",
            ["Department"] = "Engineering"
        };

        var statement = builder.BuildCreate(Entity, data);

        statement.Sql.Should().Be("INSERT INTO \"Employees\" (\"FirstName\", \"LastName\", \"Department\") VALUES (@p0, @p1, @p2)");
    }

    [Fact]
    public void BuildCreate_should_generate_identity_sql_when_primary_key_is_missing()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman"
        };

        var statement = builder.BuildCreate(Entity, data);

        statement.IdentitySql.Should().Be("SELECT last_insert_rowid();");
    }

    [Fact]
    public void BuildCreate_should_skip_identity_sql_when_primary_key_is_supplied()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["EmployeeId"] = 99,
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman"
        };

        var statement = builder.BuildCreate(Entity, data);

        statement.IdentitySql.Should().BeNull();
    }

    [Fact]
    public void BuildCreate_should_capture_parameter_values()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman"
        };

        var statement = builder.BuildCreate(Entity, data);

        statement.Parameters.Get<string>("p0").Should().Be("Asha");
        statement.Parameters.Get<string>("p1").Should().Be("Raman");
    }

    [Fact]
    public void BuildUpdate_should_generate_update_sql()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["Department"] = "Finance",
            ["Salary"] = 1000
        };

        var statement = builder.BuildUpdate(Entity, data, 42);

        statement.Sql.Should().Be("UPDATE \"Employees\" SET \"Department\" = @p0, \"Salary\" = @p1 WHERE \"EmployeeId\" = @pk");
    }

    [Fact]
    public void BuildUpdate_should_capture_primary_key_parameter()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?> { ["Department"] = "Finance" };

        var statement = builder.BuildUpdate(Entity, data, 42);

        statement.Parameters.Get<int>("pk").Should().Be(42);
    }

    [Fact]
    public void BuildSoftDelete_should_generate_update_statement()
    {
        var builder = CreateBuilder();

        var statement = builder.BuildSoftDelete(Entity, 5);

        statement.Sql.Should().Be("UPDATE \"Employees\" SET \"IsDeleted\" = @deleted WHERE \"EmployeeId\" = @pk");
        statement.Parameters.Get<int>("deleted").Should().Be(1);
    }

    [Fact]
    public void BuildCreate_should_throw_when_data_is_empty()
    {
        var builder = CreateBuilder();

        var action = () => builder.BuildCreate(Entity, new Dictionary<string, object?>());

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void BuildUpdate_should_throw_when_data_is_empty()
    {
        var builder = CreateBuilder();

        var action = () => builder.BuildUpdate(Entity, new Dictionary<string, object?>(), 1);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void BuildSoftDelete_should_throw_when_soft_delete_column_is_missing()
    {
        var builder = CreateBuilder();
        var entity = EntityMetaFactory.CreateEmployeesEntity(softDeleteColumn: null);

        var action = () => builder.BuildSoftDelete(entity, 1);

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void BuildCreate_should_quote_schema_qualified_table_names()
    {
        var builder = CreateBuilder();
        var entity = Entity with { SchemaName = "main" };

        var statement = builder.BuildCreate(entity, new Dictionary<string, object?>
        {
            ["FirstName"] = "Asha",
            ["LastName"] = "Raman"
        });

        statement.Sql.Should().StartWith("INSERT INTO \"main\".\"Employees\"");
    }

    [Theory]
    [InlineData("Sqlite", "@p0", "@pk")]
    [InlineData("SqlServer", "@p0", "@pk")]
    [InlineData("Oracle", ":p0", ":pk")]
    public void Builder_should_respect_parameter_prefixes(string provider, string valueParam, string keyParam)
    {
        var builder = new ForgeSqlBuilder(SqlDialect.ForProvider(provider));

        var statement = builder.BuildUpdate(Entity, new Dictionary<string, object?> { ["Department"] = "Ops" }, 1);

        statement.Sql.Should().Contain($"= {valueParam}");
        statement.Sql.Should().Contain($"= {keyParam}");
    }

    [Fact]
    public void BuildUpdate_should_preserve_data_order()
    {
        var builder = CreateBuilder();
        var data = new Dictionary<string, object?>
        {
            ["Department"] = "Finance",
            ["Salary"] = 2000,
            ["IsDeleted"] = 0
        };

        var statement = builder.BuildUpdate(Entity, data, 7);

        statement.Sql.Should().Contain("\"Department\" = @p0, \"Salary\" = @p1, \"IsDeleted\" = @p2");
    }

    private static ForgeSqlBuilder CreateBuilder() => new(SqlDialect.ForProvider("Sqlite"));
}
