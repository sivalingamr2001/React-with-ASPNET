# Configuration Guide for DAG Trigger System

## Overview

The DAG Trigger system now supports configuration-based setup for API URLs and authentication tokens, making it easier to manage different environments and deployments.

## Configuration Structure

### appsettings.json Configuration

```json
{
  "DagTrigger": {
    "ApiBaseUrl": "https://your-ktaiflow-instance.com/",
    "BearerToken": "your-bearer-token-here",
    "ApiKey": "alternative-api-key"
  },
  
  "KTAiFlow": {
    "ApiBaseUrl": "https://alternative-ktaiflow.com/",
    "BearerToken": "alternative-token",
    "ApiKey": "alternative-key"
  }
}
```

## Configuration Priority

The system checks configuration values in the following order:

### API Base URL:
1. `DagTrigger:ApiBaseUrl`
2. `KTAiFlow:ApiBaseUrl`
3. **Default**: `"https://nex-ae-whitelable-portal.azurewebsites.net/"`

### Authentication Token:
1. `DagTrigger:BearerToken`
2. `KTAiFlow:BearerToken`
3. `DagTrigger:ApiKey`
4. `KTAiFlow:ApiKey`
5. **Default**: `null` (no authentication)

## Environment-Specific Configuration

### Development Environment

```json
{
  "DagTrigger": {
    "ApiBaseUrl": "https://dev-ktaiflow.company.com/",
    "BearerToken": "dev-token-12345"
  }
}
```

### Staging Environment

```json
{
  "DagTrigger": {
    "ApiBaseUrl": "https://staging-ktaiflow.company.com/",
    "BearerToken": "staging-token-67890"
  }
}
```

### Production Environment

```json
{
  "DagTrigger": {
    "ApiBaseUrl": "https://prod-ktaiflow.company.com/",
    "BearerToken": "prod-token-abcdef"
  }
}
```

## Environment Variables

You can also use environment variables (useful for containerized deployments):

```bash
# Docker/Kubernetes environment variables
DagTrigger__ApiBaseUrl=https://prod-ktaiflow.com/
DagTrigger__BearerToken=your-production-token

# Alternative format
DAGTRIGGER_APIBASEURL=https://prod-ktaiflow.com/
DAGTRIGGER_BEARERTOKEN=your-production-token
```

## Azure App Service Configuration

In Azure App Service, add these application settings:

| Name | Value |
|------|-------|
| `DagTrigger:ApiBaseUrl` | `https://your-ktaiflow.azurewebsites.net/` |
| `DagTrigger:BearerToken` | `your-bearer-token` |

## Dependency Injection Setup

The configuration is automatically injected into services:

```csharp
// In Program.cs or Startup.cs
services.AddScoped<ITransactionService, TransactionService>();
services.AddScoped<FieldMapperService>();

// Configuration is automatically injected via IConfiguration
```

## Service Initialization

The DagTriggerService is automatically initialized when TransactionService is created:

```csharp
public TransactionService(
    IDataProvider transactionDataProvider,
    FieldMapperService fieldMapperService,
    DataTypeConverter dataTypeConverter,
    ILogger<TransactionService> logger,
    IConfiguration configuration,  // ← Configuration injected here
    IValidationService? validationService = null)
{
    // ... other initialization
    
    // DAG trigger service initialized with configuration
    DagTriggerService.Initialize(configuration);
}
```

## Configuration Validation

The system will log warnings if configuration is missing:

```
[DagTriggerService] Initialized with API URL: https://nex-ae-whitelable-portal.azurewebsites.net/
[DagTriggerService] Warning: No bearer token configured
```

## Backward Compatibility

The system maintains backward compatibility with explicit initialization:

```csharp
// Still supported for manual initialization
DagTriggerService.Initialize(
    "https://your-api.com/", 
    "your-token");
```

## Configuration Helper Methods

Additional helper methods are available in `ConfigHelper`:

```csharp
// Get DAG trigger API URL from configuration
string apiUrl = ConfigHelper.GetDagTriggerApiUrl();

// Get DAG trigger bearer token from configuration  
string? token = ConfigHelper.GetDagTriggerBearerToken();
```

## Testing Configuration

To test your configuration:

1. **Check logs** during application startup for initialization messages
2. **Verify API calls** are made to the correct URL
3. **Monitor authentication** success/failure in logs
4. **Use health check** endpoint if available

## Security Best Practices

1. **Never commit tokens** to source control
2. **Use environment variables** for sensitive values
3. **Rotate tokens regularly** in production
4. **Use different tokens** for different environments
5. **Monitor API usage** for unauthorized access

## Troubleshooting

### Common Issues:

1. **Missing Configuration**: Check appsettings.json exists and is deployed
2. **Wrong URL**: Verify the API base URL is correct and accessible
3. **Invalid Token**: Check token format and expiration
4. **Network Issues**: Verify firewall and network connectivity

### Debug Steps:

1. Enable detailed logging for `DagTriggerService`
2. Check configuration values at runtime
3. Test API connectivity manually
4. Verify token permissions

## Example Complete Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "KATCRUDServices.Core.Services.DagTriggerService": "Debug"
    }
  },
  
  "DagTrigger": {
    "ApiBaseUrl": "https://your-ktaiflow.com/",
    "BearerToken": "your-secure-token"
  },
  
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

This configuration provides a secure, flexible, and maintainable way to manage DAG trigger settings across different environments.