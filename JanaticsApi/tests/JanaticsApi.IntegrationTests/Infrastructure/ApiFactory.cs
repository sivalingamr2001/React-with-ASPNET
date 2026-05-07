// tests/EnterpriseApi.IntegrationTests/Infrastructure/ApiFactory.cs
using EnterpriseApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace EnterpriseApi.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("TestPassword123!")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with test database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace Redis connection
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = _redisContainer.GetConnectionString());
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Apply migrations to test database
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}