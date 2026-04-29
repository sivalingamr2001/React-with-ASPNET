using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Static service for triggering DAGs after CRUD operations
    /// Handles fire-and-forget DAG execution via KTAiFlow API
    /// Uses static HttpClient to avoid socket exhaustion
    /// No DI required - call Initialize() once during startup
    /// </summary>
    public static class DagTriggerService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _ktaiflowApiBaseUrl = string.Empty;
        private static string? _appId = null;
        private static string? _appSecret = null;
        private static string? _cachedAccessToken = null;
        private static DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
        private static readonly object _tokenLock = new object();

        /// <summary>
        /// Initialize the DAG trigger service with configuration
        /// Call this once during application startup
        /// </summary>
        public static async Task InitializeAsync(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            _ktaiflowApiBaseUrl = configuration["DagTrigger:ApiBaseUrl"] ?? "https://ijud-dev-aiflow-app.azurewebsites.net/";

            if (!_ktaiflowApiBaseUrl.EndsWith("/"))
            {
                _ktaiflowApiBaseUrl += "/";
            }

            _appId = configuration["DagTrigger:AppId"] ?? configuration["KTAiFlow:AppId"];
            _appSecret = configuration["DagTrigger:AppSecret"] ?? configuration["KTAiFlow:AppSecret"];

            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
            {
                Console.WriteLine($"[DagTriggerService] Initialized with API URL: {_ktaiflowApiBaseUrl}");
                Console.WriteLine("[DagTriggerService] Warning: AppId/AppSecret not configured");
            }
            else
            {
                // Use token-based authentication with AppId/AppSecret
                Console.WriteLine($"[DagTriggerService] Initialized with API URL: {_ktaiflowApiBaseUrl}");
                Console.WriteLine("[DagTriggerService] Using AppId/AppSecret for token-based authentication");
                Console.WriteLine("[DagTriggerService] Token will be obtained on first use (lazy initialization)");
            }
        }

        /// <summary>
        /// Initialize the DAG trigger service with configuration (synchronous wrapper)
        /// Call this once during application startup
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            InitializeAsync(configuration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initialize the DAG trigger service with explicit values
        /// </summary>
        public static void Initialize(string ktaiflowApiBaseUrl, string? appId = null, string? appSecret = null)
        {
            _ktaiflowApiBaseUrl = ktaiflowApiBaseUrl;

            // Ensure URL ends with /
            if (!_ktaiflowApiBaseUrl.EndsWith("/"))
            {
                _ktaiflowApiBaseUrl += "/";
            }

            _appId = appId;
            _appSecret = appSecret;

            Console.WriteLine($"[DagTriggerService] Initialized with API URL: {_ktaiflowApiBaseUrl}");
            if (!string.IsNullOrEmpty(_appId) && !string.IsNullOrEmpty(_appSecret))
            {
                Console.WriteLine("[DagTriggerService] AppId/AppSecret configured for token-based authentication");
            }
        }

        private static readonly SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Get a valid access token, refreshing if necessary
        /// Ensures only one token per instance and only calls API when token is missing or expired
        /// </summary>
        private static async Task<string?> GetValidTokenAsync()
        {
             if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
            {
                return null;
            }

            lock (_tokenLock)
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken) && IsTokenValid())
                {
                    Console.WriteLine($"[DagTriggerService] Using existing cached token (expires at: {_tokenExpiresAt:yyyy-MM-dd HH:mm:ss} UTC)");
                    return _cachedAccessToken;
                }
            }

            await _tokenRefreshSemaphore.WaitAsync();
            try
            {
                lock (_tokenLock)
                {
                    if (!string.IsNullOrEmpty(_cachedAccessToken) && IsTokenValid())
                    {
                        Console.WriteLine($"[DagTriggerService] Using cached token refreshed by another thread (expires at: {_tokenExpiresAt:yyyy-MM-dd HH:mm:ss} UTC)");
                        return _cachedAccessToken;
                    }
                }
                Console.WriteLine("[DagTriggerService] Token missing or expired. Generating new token via API...");
                await RefreshTokenAsync();

                lock (_tokenLock)
                {
                    return _cachedAccessToken;
                }
            }
            finally
            {
                _tokenRefreshSemaphore.Release();
            }
        }

        /// <summary>
        /// Check if the current cached token is still valid (with 5 minute buffer for safety)
        /// </summary>
        private static bool IsTokenValid()
        {
           return _tokenExpiresAt.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5);
        }

        /// <summary>
        /// Refresh the access token by calling the KTAiFlow auth endpoint
        /// </summary>
        private static async Task RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
            {
                throw new InvalidOperationException("AppId and AppSecret must be configured to use token-based authentication");
            }

            try
            {
                var authUrl = $"{_ktaiflowApiBaseUrl}api/auth/token";

                var requestBody = new
                {
                    AppId = _appId,
                    AppSecret = _appSecret
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[DagTriggerService] Requesting access token from: {authUrl}");

                var response = await _httpClient.PostAsync(authUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to obtain access token. Status: {response.StatusCode}, Error: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (tokenResponse == null || tokenResponse?.accessToken == null)
                {
                    throw new InvalidOperationException("Invalid token response from auth endpoint");
                }

                var accessToken = tokenResponse?.accessToken.ToString();

                lock (_tokenLock)
                {
                    _cachedAccessToken = accessToken;
                    _tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(100);
                }

                Console.WriteLine($"[DagTriggerService] Access token obtained successfully. Expires at: {_tokenExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerService] Error refreshing access token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Trigger a DAG asynchronously (fire and forget)
        /// </summary>
        public static async Task TriggerDagAsync(
            Guid dagId,
            string tableName,
            object recordId,
            Dictionary<string, object>? additionalContext = null,
            Func<string, string?, string?, int?, int?, Task>? onExecutionComplete = null)
        {
            try
            {
                Console.WriteLine(
                    $"[DagTriggerService] Triggering DAG {dagId} for table {tableName}, record {recordId}");

                // Build input data for DAG
                var inputData = new Dictionary<string, object>
                {
                    { "tableName", tableName },
                    { "recordId", recordId }
                };

                // Add additional context if provided
                if (additionalContext != null)
                {
                    foreach (var kvp in additionalContext)
                    {
                        inputData[kvp.Key] = kvp.Value;
                    }
                }

                // Call KTAiFlow API to execute DAG
                var json = JsonConvert.SerializeObject(inputData, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_ktaiflowApiBaseUrl}api/dag-config/{dagId:D}/execute";

                Console.WriteLine($"[DagTriggerService] Calling KTAiFlow API: {apiUrl}");

                // Get valid token (will refresh if needed)
                var token = await GetValidTokenAsync();

                // Create request with bearer token if configured
                using (var request = new HttpRequestMessage(HttpMethod.Post, apiUrl))
                {
                    request.Content = content;

                    // Add bearer token authorization if configured
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await _httpClient.SendAsync(request);
                    stopwatch.Stop();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var executionTimeMs = (int)stopwatch.ElapsedMilliseconds;

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            $"[DagTriggerService] DAG {dagId} triggered successfully for table {tableName}");

                        // Log successful execution
                        if (onExecutionComplete != null)
                        {
                            await onExecutionComplete("Success", responseContent, null, (int)response.StatusCode, executionTimeMs);
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[DagTriggerService] Failed to trigger DAG {dagId}: {response.StatusCode} - {responseContent}");

                        // Log failed execution
                        if (onExecutionComplete != null)
                        {
                            await onExecutionComplete("Failed", responseContent, $"HTTP {response.StatusCode}", (int)response.StatusCode, executionTimeMs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerService] Error triggering DAG {dagId} for table {tableName}: {ex.Message}");

                // Log error execution
                if (onExecutionComplete != null)
                {
                    await onExecutionComplete("Failed", null, ex.Message, null, null);
                }

                // Don't throw - we don't want to fail the original transaction
            }
        }

        /// <summary>
        /// Check if DAG service is healthy
        /// </summary>
        public static async Task<bool> IsHealthyAsync()
        {
            try
            {
                var healthUrl = $"{_ktaiflowApiBaseUrl}health";

                // Get valid token (will refresh if needed)
                var token = await GetValidTokenAsync();

                // Create request with bearer token if configured
                using (var request = new HttpRequestMessage(HttpMethod.Get, healthUrl))
                {
                    // Add bearer token authorization if configured
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    var response = await _httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerService] Error checking KTAiFlow health: {ex.Message}");
                return false;
            }
        }
    }
}
