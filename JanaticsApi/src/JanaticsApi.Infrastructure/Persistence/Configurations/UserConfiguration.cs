// src/EnterpriseApi.Infrastructure/Persistence/Configurations/UserConfiguration.cs
using JanaticsApi.Domain.Entities.Users;
using JanaticsApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JanaticsApi.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever(); // Guid generated in domain, not DB

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        // Value Object owned entity mapping
        builder.OwnsOne(u => u.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            emailBuilder.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Soft delete column
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        // Performance indexes
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => new { u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_Users_Active_NotDeleted");

        // Ignore domain events — not persisted
        builder.Ignore(u => u.DomainEvents);
    }
}