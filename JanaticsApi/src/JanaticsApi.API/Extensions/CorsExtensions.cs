// src/EnterpriseApi.API/Extensions/CorsExtensions.cs
public static class CorsExtensions
{
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                if (allowedOrigins.Length > 0)
                    policy.WithOrigins(allowedOrigins);
                else
                    policy.AllowAnyOrigin(); // Development only

                policy
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
                    .WithExposedHeaders("X-Correlation-ID", "X-Total-Count", "Link");
            });
        });

        return services;
    }
}