// src/EnterpriseApi.API/Extensions/AuthenticationExtensions.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace JanaticsApi.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = false; // Don't save token in HttpContext — use claims
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Zero tolerance for clock drift
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    // Check token revocation list in Redis
                    var jti = context.Principal?.FindFirst("jti")?.Value;
                    if (jti is not null)
                    {
                        var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                        var isRevoked = await cache.ExistsAsync(CacheKeys.Auth.RevokedToken(jti));

                        if (isRevoked)
                            context.Fail("Token has been revoked.");
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.RequireAuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(PolicyNames.RequireAdminRole, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(PolicyNames.RequireManagerOrAdmin, policy =>
                policy.RequireRole("Manager", "Admin"));

            // Permission-based policy example
            options.AddPolicy(PolicyNames.CanManageUsers, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(AppClaimTypes.Permission, "users:write")));
        });

        return services;
    }
}