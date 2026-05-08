using Janatics.DataEngine.Api;
using Janatics.DataEngine.Infrastructure.DependencyInjection;
using Janatics.DataEngine.QueryEngine.DependencyInjection;
using Janatics.DataEngine.Security.DependencyInjection;
using Janatics.DataEngine.TransactionEngine.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDataEngineInfrastructure(builder.Configuration);
builder.Services.AddDataEngineSecurity();
builder.Services.AddDataEngineQueryEngine();
builder.Services.AddDataEngineTransactionEngine();

var app = builder.Build();

await app.Services.SeedDataEngineMetadataAsync();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "Janatics.DataEngine.Api",
    status = "running"
}));

app.Run();
