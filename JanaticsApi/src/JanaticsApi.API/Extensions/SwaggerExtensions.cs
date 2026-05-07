// src/EnterpriseApi.API/Extensions/SwaggerExtensions.cs
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JanaticsApi.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Document each API version separately
            var provider = services.BuildServiceProvider()
                .GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = $"Enterprise API {description.GroupName.ToUpperInvariant()}",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated
                        ? "**DEPRECATED** — This API version is deprecated. Migrate to the latest version."
                        : "Enterprise-grade ASP.NET Core Web API",
                    Contact = new OpenApiContact
                    {
                        Name = "API Engineering Team",
                        Email = "api-team@enterprise.com"
                    }
                });
            }

            // JWT security definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT token. Example: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });

            // Include XML documentation comments
            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.EnableAnnotations();
            options.UseInlineDefinitionsForEnums();
        });

        return services;
    }

    public static WebApplication UseSwaggerWithVersioning(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/openapi.json";
        });

        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/api-docs/{description.GroupName}/openapi.json",
                    $"Enterprise API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "api-docs";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
        });

        return app;
    }
}