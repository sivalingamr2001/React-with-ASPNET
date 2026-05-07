// src/EnterpriseApi.API/Program.cs
using JanaticsApi.API.Endpoints;
using JanaticsApi.API.Extensions;
using JanaticsApi.API.Middleware;
using JanaticsApi.Application;
using JanaticsApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — configured before anything else to capture startup errors
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Service registrations
builder.Services
    .AddApplicationServices()                           // Application layer
    .AddInfrastructureServices(builder.Configuration)  // Infrastructure layer
    .AddApiServices(builder.Configuration)             // API layer (auth, versioning, swagger, etc.)
    .AddEndpoints(typeof(Program).Assembly);           // Minimal API endpoints

var app = builder.Build();

// Run database migrations and seed (development only)
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
}

// Middleware pipeline order matters — sequence is intentional
app.UseCorrelationId();
app.UseSecurityHeaders();
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithVersioning();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLogging();

// Map endpoints
app.MapEndpoints();
app.MapHealthChecks();
app.MapMetrics(); // Prometheus endpoint

await app.RunAsync();