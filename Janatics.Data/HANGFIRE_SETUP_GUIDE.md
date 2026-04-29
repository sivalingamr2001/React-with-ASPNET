# Hangfire DAG Trigger Setup Guide

## Overview

The DAG trigger logic has been extracted into a separate method (`DagTriggerJobService.ProcessDagTriggersAsync`) that is called via Hangfire background jobs. This provides:

- **True fire-and-forget**: Transaction commits immediately, DAG triggers run in background
- **Reliability**: Jobs are persisted and survive application restarts
- **Retry logic**: Automatic retry with exponential backoff (3 attempts: 10s, 30s, 60s)
- **Data visibility**: 2-second delay ensures database has committed data
- **Monitoring**: Hangfire dashboard for job tracking

## Changes Made

### 1. New File: `Services/DagTriggerJobService.cs`
- Contains `ProcessDagTriggersAsync()` method with all DAG trigger logic
- Checks for available triggers using `DagTriggerRepository.GetTriggersForTableAsync()`
- Processes both main table and child table triggers
- Includes automatic retry with `[AutomaticRetry]` attribute

### 2. Updated: `Services/TransactionService.cs`
- Replaced inline DAG trigger logic with Hangfire job enqueue
- Serializes data for Hangfire job persistence
- Schedules job with 2-second delay using `BackgroundJob.Schedule()`
- Transaction commits immediately without waiting for DAG triggers

### 3. Updated: `KATCRUDServices.Core.csproj`
- Added `Hangfire.Core` package (v1.8.9)

## Setup Instructions

### Step 1: Install Additional Hangfire Packages in Your API Project

In your main API project (not the Core library), add these packages:

```bash
dotnet add package Hangfire.AspNetCore --version 1.8.9
dotnet add package Hangfire.PostgreSql --version 1.20.8
# OR for SQL Server:
# dotnet add package Hangfire.SqlServer --version 1.8.9
```

### Step 2: Configure Hangfire in Program.cs or Startup.cs

```csharp
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configure Hangfire with PostgreSQL storage
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection"));
    }, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        PrepareSchemaIfNecessary = true
    }));

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent background workers
    options.Queues = new[] { "default" };
    options.ServerName = "DAG-Trigger-Server";
});

var app = builder.Build();

// Optional: Add Hangfire Dashboard for monitoring
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Add authorization if needed
    // Authorization = new[] { new MyAuthorizationFilter() }
});

app.Run();
```

### Step 3: Add Connection String

Add Hangfire connection string to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Host=localhost;Database=mydb;Username=user;Password=pass"
  }
}
```

### Step 4: Initialize DAG Trigger Service

In your startup code, initialize the DAG trigger service:

```csharp
// Initialize DAG trigger service with KTAiFlow API configuration
DagTriggerService.Initialize(
    ktaiflowApiBaseUrl: "https://your-ktaiflow-api.com/",
    bearerToken: "your-bearer-token");
```

## How It Works

### Before (Blocking):
1. Transaction commits
2. DAG triggers execute inline (blocks response)
3. API returns after all DAGs triggered

### After (Non-Blocking with Hangfire):
1. Transaction commits
2. Hangfire job enqueued (returns immediately)
3. **API returns instantly** ✅
4. Hangfire worker picks up job after 2 seconds
5. Job checks for available triggers
6. Job executes DAG triggers in background
7. Automatic retry if job fails

## Configuration Options

### Adjust Delay

Change the delay before DAG triggers execute:

```csharp
// In TransactionService.cs, line ~170
var jobId = BackgroundJob.Schedule(
    () => DagTriggerJobService.ProcessDagTriggersAsync(...),
    TimeSpan.FromSeconds(5)); // Change from 2 to 5 seconds
```

### Adjust Retry Policy

Modify retry attempts in `DagTriggerJobService.cs`:

```csharp
[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10, 30, 60, 120, 300 })]
public static async Task ProcessDagTriggersAsync(...)
```

### Worker Count

Adjust concurrent workers in Hangfire configuration:

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10; // Increase for more concurrent jobs
});
```

## Monitoring

Access Hangfire Dashboard at: `https://your-api-url/hangfire`

The dashboard shows:
- Enqueued jobs
- Processing jobs
- Succeeded jobs
- Failed jobs (with retry attempts)
- Scheduled jobs (waiting for delay)

## Testing

1. **Test Transaction**: Create a transaction and verify it returns immediately
2. **Check Hangfire Dashboard**: Verify job is scheduled with 2-second delay
3. **Monitor Job Execution**: Watch job move from Scheduled → Processing → Succeeded
4. **Verify DAG Execution**: Check DAG trigger logs in `kat_dag_trigger_logs` table
5. **Test Retry**: Simulate failure and verify automatic retry

## Troubleshooting

### Jobs Not Processing
- Check Hangfire server is running: `builder.Services.AddHangfireServer()`
- Verify connection string is correct
- Check worker count > 0

### Jobs Failing
- Check Hangfire dashboard for error details
- Verify DAG trigger service is initialized
- Check KTAiFlow API is accessible
- Review logs in `kat_dag_trigger_logs` table

### Data Not Visible to DAG
- Increase delay: `TimeSpan.FromSeconds(5)` or higher
- Check database replication lag
- Verify transaction is actually committing

## Benefits

✅ **Instant API Response**: Transaction returns immediately  
✅ **Reliable Execution**: Jobs persisted in database  
✅ **Automatic Retry**: Failed jobs retry automatically  
✅ **Data Visibility**: 2-second delay ensures data is committed  
✅ **Monitoring**: Dashboard for job tracking  
✅ **Scalability**: Multiple workers process jobs concurrently  
✅ **Fault Tolerance**: Jobs survive application restarts  

## Migration Notes

- No changes needed to existing API calls
- DAG trigger behavior is identical, just non-blocking
- Existing trigger configurations work as-is
- Logs still written to `kat_dag_trigger_logs` table
