using Janatics.DataEngine.QueryEngine.Execution;
using Janatics.DataEngine.QueryEngine.Graph;
using Janatics.DataEngine.QueryEngine.Pipeline;
using Janatics.DataEngine.QueryEngine.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.QueryEngine.DependencyInjection;

public static class QueryEngineServiceExtensions
{
    public static IServiceCollection AddDataEngineQueryEngine(this IServiceCollection services)
    {
        services.AddScoped<DataEngineQueryService>();
        services.AddScoped<IFetchPipeline, FetchPipeline>();
        services.AddScoped<IDbQueryExecutor, AdoDbQueryExecutor>();
        services.AddScoped<IChildCollectionLoader, ChildCollectionLoader>();
        services.AddScoped<IObjectGraphBuilder, ObjectGraphBuilder>();
        services.AddScoped<IFetchPipelineStage, QueryResolutionStage>();
        services.AddScoped<IFetchPipelineStage, ParameterBindingStage>();
        services.AddScoped<IFetchPipelineStage, SqlValidationStage>();
        services.AddScoped<IFetchPipelineStage, ExecutionStage>();
        services.AddScoped<IFetchPipelineStage, ProjectionStage>();
        return services;
    }
}
