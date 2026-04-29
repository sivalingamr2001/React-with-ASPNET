using FluentAssertions;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;

namespace Janatics.DataEngine.Tests;

public sealed class SqlDialectTests
{
    [Theory]
    [InlineData("Sqlite", "Sqlite")]
    [InlineData("sqlite", "Sqlite")]
    [InlineData("SQLSERVER", "SqlServer")]
    [InlineData("oracle", "Oracle")]
    public void ForProvider_should_resolve_expected_provider_name(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.ProviderName.Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", "\"Employees\"")]
    [InlineData("SqlServer", "[Employees]")]
    [InlineData("Oracle", "\"Employees\"")]
    public void QuoteIdentifier_should_use_provider_specific_wrapping(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.QuoteIdentifier("Employees").Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", "\"dbo\".\"Employees\"")]
    [InlineData("SqlServer", "[dbo].[Employees]")]
    [InlineData("Oracle", "\"dbo\".\"Employees\"")]
    public void QuoteIdentifier_should_handle_multi_part_identifiers(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.QuoteIdentifier("dbo.Employees").Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", "@p0")]
    [InlineData("SqlServer", "@p0")]
    [InlineData("Oracle", ":p0")]
    public void Parameter_should_apply_provider_prefix(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.Parameter("p0").Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", " LIMIT @limit OFFSET @offset")]
    [InlineData("SqlServer", " OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY")]
    [InlineData("Oracle", " OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY")]
    public void ApplyPaging_should_emit_expected_clause(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.ApplyPaging(string.Empty, "offset", "limit").Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", "@offset")]
    [InlineData("Oracle", ":limit")]
    public void Parameter_should_trim_existing_prefixes(string provider, string expected)
    {
        var dialect = SqlDialect.ForProvider(provider);

        dialect.Parameter(provider == "Oracle" ? "@limit" : ":offset").Should().Be(expected);
    }

    [Fact]
    public void ForProvider_should_throw_for_unknown_provider()
    {
        var action = () => SqlDialect.ForProvider("Postgres");

        action.Should().Throw<SqlBuildException>();
    }

    [Fact]
    public void QuoteIdentifier_should_throw_for_empty_identifier()
    {
        var dialect = SqlDialect.ForProvider("Sqlite");

        var action = () => dialect.QuoteIdentifier(string.Empty);

        action.Should().Throw<SqlBuildException>();
    }

    [Theory]
    [InlineData("Sqlite", "sqlite")]
    [InlineData("SQL SERVER", "sqlserver")]
    [InlineData("mssql", "sqlserver")]
    [InlineData("Oracle.ManagedDataAccess.Client", "oracle")]
    public void ProviderDetector_should_normalize_aliases(string input, string expected)
    {
        ProviderDetector.Normalize(input).Should().Be(expected);
    }
}
