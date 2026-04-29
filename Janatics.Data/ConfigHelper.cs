using Microsoft.Extensions.Configuration;
public static class ConfigHelper
{
    private static IConfigurationRoot _configuration;

    static ConfigHelper()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public static string GetApiUrl()
    {
        return _configuration["ApprovalProcessURL"] ?? "https://nex-ae-whitelable-portal.azurewebsites.net/api/dag-config/378fe21b-b4b6-4558-840e-5370be7b7679/execute";
    }

    public static string GetDagTriggerApiUrl()
    {
        return _configuration["DagTrigger:ApiBaseUrl"] ?? 
               _configuration["KTAiFlow:ApiBaseUrl"] ?? 
               "https://nex-ae-whitelable-portal.azurewebsites.net/";
    }

    public static string? GetDagTriggerBearerToken()
    {
        return _configuration["DagTrigger:BearerToken"] ?? 
               _configuration["KTAiFlow:BearerToken"] ?? 
               _configuration["DagTrigger:ApiKey"] ??
               _configuration["KTAiFlow:ApiKey"];
    }
}
