

using JanaticsApi.Domain.Common;
using JanaticsApi.Domain.ValueObjects;

namespace JanaticsApi.Domain.Entities.Users;

public sealed class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    // Private parameterless constructor for EF Core
    private User() { }

    public static User Create(
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        UserRole role = UserRole.User)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, user.Email.Value));

        return user;
    }

    public void SetRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
            RaiseDomainEvent(new UserAccountLockedEvent(Id, LockedUntil.Value));
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil > DateTimeOffset.UtcNow;

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolationException("User is already deactivated.");

        IsActive = false;
        RevokeRefreshToken();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new UserDeactivatedEvent(Id));
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}