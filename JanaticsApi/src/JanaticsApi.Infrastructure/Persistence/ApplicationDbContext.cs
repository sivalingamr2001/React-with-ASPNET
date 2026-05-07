// src/EnterpriseApi.Infrastructure/Persistence/ApplicationDbContext.cs
using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Domain.Common;
using JanaticsApi.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace JanaticsApi.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters — applied to all queries unless explicitly disabled
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging(false); // Never in production
        optionsBuilder.EnableDetailedErrors(false);       // Never in production
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set audit fields before saving
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedAt(DateTimeOffset.UtcNow, _currentUserService.UserId);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdatedAt(DateTimeOffset.UtcNow, _currentUserService.UserId);
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}