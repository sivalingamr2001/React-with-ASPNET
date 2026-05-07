// tests/EnterpriseApi.UnitTests/Domain/Users/UserTests.cs
using EnterpriseApi.Domain.Entities.Users;
using EnterpriseApi.Domain.Exceptions;
using EnterpriseApi.Domain.ValueObjects;

namespace EnterpriseApi.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldRaiseDomainEvent()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        var user = User.Create("John", "Doe", email, "hashedPassword");

        // Assert
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>();
    }

    [Fact]
    public void Deactivate_AlreadyDeactivatedUser_ShouldThrowBusinessRuleViolation()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        // Act
        var act = () => user.Deactivate();

        // Assert
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*already deactivated*");
    }

    [Theory]
    [InlineData(4, false)] // 4 failed attempts — not locked yet
    [InlineData(5, true)]  // 5 failed attempts — locked
    public void RecordFailedLoginAttempt_ShouldLockAfterFiveAttempts(
        int attempts, bool expectedLocked)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (var i = 0; i < attempts; i++)
            user.RecordFailedLoginAttempt();

        // Assert
        user.IsLocked().Should().Be(expectedLocked);
    }

    private static User CreateTestUser() =>
        User.Create("John", "Doe", Email.Create("test@example.com"), "hash");
}