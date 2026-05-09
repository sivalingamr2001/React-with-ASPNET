using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Janatics.DataEngine;
using Janatics.DataEngine.Api.Auth;
using Janatics.DataEngine.Api.Infrastructure;
using Janatics.DataEngine.Api.Middleware;
using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.Models.RequestModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IIdempotencyService, IdempotencyService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("fetch.read", policy => policy.RequireAssertion(ctx => HasScope(ctx.User, "fetch.read")));
    options.AddPolicy("fetch.write", policy => policy.RequireAssertion(ctx => HasScope(ctx.User, "fetch.write")));
    options.AddPolicy("process.execute", policy => policy.RequireAssertion(ctx => HasScope(ctx.User, "process.execute")));
    options.AddPolicy("metadata.read", policy => policy.RequireAssertion(ctx => HasScope(ctx.User, "metadata.read")));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "JWT issuer is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "JWT audience is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "JWT signing key is required.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddRateLimiter(_ => { });

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Janatics.DataEngine.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Janatics.DataEngine"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation());

builder.Services.AddJanaticsDataEngine(options =>
{
    builder.Configuration.GetSection("DataEngine").Bind(options);
});

var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "janatics:dataengine:";
    });
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler("/error");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapGet("/error", () => Results.Problem("Unhandled server error.")).AllowAnonymous();

var v1 = app.NewVersionedApi("DataEngine").MapGroup("/api/v{version:apiVersion}").HasApiVersion(1.0);

v1.MapPost("/fetch/queries", [Authorize("fetch.write")] async (DataEngine engine, SaveQueryDefinitionRequest request, CancellationToken ct) =>
{
    var result = await engine.SaveFetchQueryAsync(request, ct).ConfigureAwait(false);
    return Results.Ok(result);
});

v1.MapGet("/fetch/queries/{queryNumber:long}", [Authorize("fetch.read")] async (DataEngine engine, long queryNumber, CancellationToken ct) =>
{
    var result = await engine.GetFetchQueryAsync(queryNumber, ct).ConfigureAwait(false);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

v1.MapGet("/fetch/queries", [Authorize("fetch.read")] async (DataEngine engine, CancellationToken ct) =>
{
    var result = await engine.GetFetchQueriesAsync(ct).ConfigureAwait(false);
    return Results.Ok(result);
});

v1.MapDelete("/fetch/queries/{queryNumber:long}", [Authorize("fetch.write")] async (DataEngine engine, long queryNumber, ClaimsPrincipal user, CancellationToken ct) =>
{
    var deletedBy = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    var deleted = await engine.DeleteFetchQueryAsync(queryNumber, deletedBy, ct).ConfigureAwait(false);
    return deleted ? Results.NoContent() : Results.NotFound();
});

v1.MapPost("/fetch/execute", [Authorize("fetch.read")] async (DataEngine engine, FetchExecutionRequest request, ClaimsPrincipal user, CancellationToken ct) =>
{
    request.ExecutedBy ??= user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await engine.ExecuteFetchAsync(request, ct).ConfigureAwait(false);
    return Results.Ok(result);
});

v1.MapGet("/metadata/tables", [Authorize("metadata.read")] async (DataEngine engine, CancellationToken ct) =>
{
    var result = await engine.GetFetchTablesAsync(ct).ConfigureAwait(false);
    return Results.Ok(result);
});

v1.MapGet("/metadata/tables/{tableName}/columns", [Authorize("metadata.read")] async (DataEngine engine, string tableName, CancellationToken ct) =>
{
    var result = await engine.GetFetchTableColumnsAsync(tableName, ct).ConfigureAwait(false);
    return Results.Ok(result);
});

v1.MapPost("/process/execute", [Authorize("process.execute")] async (HttpContext httpContext, DataEngine engine, IIdempotencyService idempotencyService, ProcessRequest request, CancellationToken ct) =>
{
    if (!httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var idemHeader) || string.IsNullOrWhiteSpace(idemHeader))
        return Results.BadRequest(new { message = "X-Idempotency-Key header is required for process execution." });

    var requestJson = JsonSerializer.Serialize(request);
    var hash = IdempotencyService.ComputeRequestHash(requestJson);
    var scopedKey = idempotencyService.BuildScopedKey(httpContext, idemHeader.ToString(), hash);
    var cached = await idempotencyService.TryGetResponseAsync(scopedKey, ct).ConfigureAwait(false);
    if (!string.IsNullOrWhiteSpace(cached))
        return Results.Content(cached, "application/json");

    await engine.ExecuteAsync(request).ConfigureAwait(false);
    var responseJson = JsonSerializer.Serialize(new { accepted = true, request.RootEntityName, request.RootEntityId });
    await idempotencyService.SaveResponseAsync(scopedKey, responseJson, TimeSpan.FromHours(24), ct).ConfigureAwait(false);
    return Results.Content(responseJson, "application/json");
});

app.Run();

static bool HasScope(ClaimsPrincipal principal, string expectedScope)
{
    return principal.Claims
        .Where(c => c.Type is "scope" or "scp" or ClaimTypes.Role)
        .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Any(value => string.Equals(value, expectedScope, StringComparison.OrdinalIgnoreCase));
}
