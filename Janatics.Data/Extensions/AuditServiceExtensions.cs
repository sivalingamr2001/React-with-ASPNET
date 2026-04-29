using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KATCRUDServices.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering audit services
    /// </summary>
    public static class AuditServiceExtensions
    {
        /// <summary>
        /// Registers all audit-related services
        /// </summary>
        public static IServiceCollection AddAuditLogging(this IServiceCollection services, int queueCapacity = 10000)
        {
            // Register queue as singleton
            services.AddSingleton<AuditQueue>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditQueue>>();
                return new AuditQueue(logger, queueCapacity);
            });

            // Register IAuditQueue interface
            services.AddSingleton<IAuditQueue>(sp => sp.GetRequiredService<AuditQueue>());

            // Register background service
            services.AddHostedService<AuditBackgroundService>();

            // Register services
            services.AddScoped<AuditWriter>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<AuditDiffBuilder>();

            return services;
        }
    }
}
