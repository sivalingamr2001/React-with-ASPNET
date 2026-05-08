using Janatics.DataEngine.TransactionEngine.Commands;
using Janatics.DataEngine.TransactionEngine.Orchestration;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.TransactionEngine.DependencyInjection;

public static class TransactionEngineServiceExtensions
{
    public static IServiceCollection AddDataEngineTransactionEngine(this IServiceCollection services)
    {
        services.AddScoped<TransactionEngineService>();
        services.AddScoped<IOperationDetector, OperationDetector>();
        services.AddScoped<INodeProcessor, NodeProcessor>();
        services.AddScoped<IInsertCommandBuilder, InsertCommandBuilder>();
        services.AddScoped<IUpdateCommandBuilder, UpdateCommandBuilder>();
        services.AddScoped<IDeleteCommandBuilder, DeleteCommandBuilder>();
        return services;
    }
}
