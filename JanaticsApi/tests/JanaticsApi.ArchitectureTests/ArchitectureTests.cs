// tests/EnterpriseApi.ArchitectureTests/ArchitectureTests.cs
using NetArchTest.Rules;

namespace EnterpriseApi.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly =
        typeof(EnterpriseApi.Domain.Common.BaseEntity).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(EnterpriseApi.Application.DependencyInjection).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(EnterpriseApi.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_Should_Not_HaveDependency_On_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Application layer.");
    }

    [Fact]
    public void Domain_Should_Not_HaveDependency_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_HaveDependency_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void CommandHandlers_Should_BeSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ImplementInterface(typeof(ICommandHandler<,>))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Entities_Should_HavePrivateConstructors()
    {
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That().Inherit(typeof(BaseEntity))
            .GetTypes();

        foreach (var type in entityTypes)
        {
            var hasPublicConstructor = type.GetConstructors(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance).Length > 0;

            hasPublicConstructor.Should().BeFalse(
                because: $"{type.Name} should use factory methods, not public constructors.");
        }
    }
}