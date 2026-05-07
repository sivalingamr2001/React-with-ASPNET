using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Caching;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Domain.Services;
using JanaticsApi.Infrastructure.Authentication;
using JanaticsApi.Infrastructure.Caching;
using JanaticsApi.Infrastructure.Persistence;
using JanaticsApi.Infrastructure.Persistence.Repositories;
using JanaticsApi.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace JanaticsApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")!));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        return services;
    }
}
