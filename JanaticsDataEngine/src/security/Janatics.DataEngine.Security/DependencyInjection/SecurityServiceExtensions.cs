using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.Security.Sanitization;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.Security.DependencyInjection;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddDataEngineSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IQueryValidator, AllowedOperationValidator>();
        services.AddSingleton<ISqlSanitizer, SqlSanitizer>();
        return services;
    }
}
