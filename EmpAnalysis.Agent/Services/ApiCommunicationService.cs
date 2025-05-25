using System.Text;
using System.Text.Json;
using EmpAnalysis.Agent.Configuration;
using EmpAnalysis.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmpAnalysis.Agent.Services;

public interface IApiCommunicationService
{
    Task<bool> SendMonitoringDataAsync(MonitoringSession session);
    Task<bool> SendScreenshotAsync(ScreenshotCapture screenshot);
    Task<bool> SendActivityDataAsync(List<ApplicationUsage> applications, List<WebsiteVisit> websites);
    Task<bool> SendSystemEventsAsync(List<SystemEvent> systemEvents);
    Task<bool> CheckConnectionAsync();
    Task<AgentRegistrationResponse?> RegisterAgentAsync(string employeeId, string machineName);
    Task<bool> SendHeartbeatAsync(string agentId, string employeeId);
    Task<AgentConfigurationResponse?> GetConfigurationAsync(string agentId);
}

public class ApiCommunicationService : IApiCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _settings;
    private readonly ILogger<ApiCommunicationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiCommunicationService(
        HttpClient httpClient,
        IOptions<AgentSettings> settings,
        ILogger<ApiCommunicationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value.ApiSettings;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = _settings.Timeout;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EmpAnalysis-Agent/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        // Configure SSL certificate validation bypass for development
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
    }

    public async Task<bool> SendMonitoringDataAsync(MonitoringSession session)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var json = JsonSerializer.Serialize(session, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/monitoring/session", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Monitoring session sent successfully for employee {session.EmployeeId}");
                return true;
            }

            _logger.LogWarning($"Failed to send monitoring session. Status: {response.StatusCode}");
            return false;
        });
    }

    public async Task<bool> SendScreenshotAsync(ScreenshotCapture screenshot)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var payload = new
            {
                employeeId = GetEmployeeId(),
                imageData = screenshot.Base64Data,
                capturedAt = screenshot.Timestamp,
                windowTitle = screenshot.ActiveWindow,
                applicationName = screenshot.ActiveApplication,
                isBlurred = false
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/monitoring/screenshot", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Screenshot sent successfully. Size: {screenshot.FileSize} bytes");
                return true;
            }

            _logger.LogWarning($"Failed to send screenshot. Status: {response.StatusCode}");
            return false;
        });
    }

    public async Task<bool> SendActivityDataAsync(List<ApplicationUsage> applications, List<WebsiteVisit> websites)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var payload = new
            {
                employeeId = GetEmployeeId(),
                activityType = "ApplicationUsage",
                description = $"Activity report: {applications.Count} applications, {websites.Count} websites",
                timestamp = DateTime.UtcNow,
                applicationName = applications.FirstOrDefault()?.ApplicationName ?? "Multiple Applications",
                duration = (long)applications.Sum(a => a.Duration.TotalMilliseconds),
                isProductiveActivity = applications.Any(a => a.IsProductiveApp)
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/monitoring/activity", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Activity data sent successfully. Apps: {applications.Count}, Websites: {websites.Count}");
                return true;
            }

            _logger.LogWarning($"Failed to send activity data. Status: {response.StatusCode}");
            return false;
        });
    }

    public async Task<bool> SendSystemEventsAsync(List<SystemEvent> systemEvents)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            foreach (var systemEvent in systemEvents)
            {
                var payload = new
                {
                    employeeId = GetEmployeeId(),
                    activityType = "SystemEvent",
                    description = systemEvent.Description,
                    timestamp = systemEvent.Timestamp,
                    applicationName = "System",
                    duration = 0,
                    isProductiveActivity = false
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/monitoring/activity", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to send system event. Status: {response.StatusCode}");
                    return false;
                }
            }
            
            _logger.LogDebug($"System events sent successfully. Count: {systemEvents.Count}");
            return true;
        });
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("API connection check successful");
                return true;
            }

            _logger.LogWarning($"API connection check failed. Status: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API connection check failed with exception");
            return false;
        }
    }

    public async Task<AgentRegistrationResponse?> RegisterAgentAsync(string employeeId, string machineName)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var payload = new
            {
                employeeId = employeeId,
                machineName = machineName,
                agentVersion = "1.0.0",
                operatingSystem = Environment.OSVersion.ToString(),
                registrationTime = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/agent/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var registrationResponse = JsonSerializer.Deserialize<AgentRegistrationResponse>(responseContent, _jsonOptions);
                
                _logger.LogInformation($"Agent registered successfully with ID: {registrationResponse?.AgentId}");
                return registrationResponse;
            }

            _logger.LogWarning($"Agent registration failed. Status: {response.StatusCode}");
            return null;
        });
    }

    public async Task<bool> SendHeartbeatAsync(string agentId, string employeeId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var payload = new
            {
                agentId = agentId,
                employeeId = employeeId,
                timestamp = DateTime.UtcNow,
                status = "Online"
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/agent/heartbeat", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Heartbeat sent successfully for agent {agentId}");
                return true;
            }

            _logger.LogWarning($"Heartbeat failed. Status: {response.StatusCode}");
            return false;
        });
    }

    public async Task<AgentConfigurationResponse?> GetConfigurationAsync(string agentId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync($"/agent/config/{agentId}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var configResponse = JsonSerializer.Deserialize<AgentConfigurationResponse>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Configuration retrieved successfully for agent {agentId}");
                return configResponse;
            }

            _logger.LogWarning($"Configuration retrieval failed. Status: {response.StatusCode}");
            return null;
        });
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> operation) where T : class
    {
        var attempt = 0;
        var maxAttempts = _settings.RetryAttempts;

        while (attempt < maxAttempts)
        {
            try
            {
                attempt++;
                var result = await operation();
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"HTTP request failed (attempt {attempt}/{maxAttempts}): {ex.Message}");
                
                if (attempt >= maxAttempts)
                {
                    _logger.LogError($"Operation failed after {maxAttempts} attempts");
                    throw;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Request timeout (attempt {attempt}/{maxAttempts}): {ex.Message}");
                
                if (attempt >= maxAttempts)
                {
                    _logger.LogError($"Operation timed out after {maxAttempts} attempts");
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        return null;
    }

    private async Task<bool> ExecuteWithRetryAsync(Func<Task<bool>> operation)
    {
        var attempt = 0;
        var maxAttempts = _settings.RetryAttempts;

        while (attempt < maxAttempts)
        {
            try
            {
                attempt++;
                return await operation();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"HTTP request failed (attempt {attempt}/{maxAttempts}): {ex.Message}");
                
                if (attempt >= maxAttempts)
                {
                    _logger.LogError($"Operation failed after {maxAttempts} attempts");
                    return false;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Request timeout (attempt {attempt}/{maxAttempts}): {ex.Message}");
                
                if (attempt >= maxAttempts)
                {
                    _logger.LogError($"Operation timed out after {maxAttempts} attempts");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        return false;
    }

    private static string GetEmployeeId()
    {
        return Environment.UserName ?? "unknown";
    }
}

public class AgentRegistrationResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class AgentConfigurationResponse
{
    public int ScreenshotInterval { get; set; }
    public int ActivityInterval { get; set; }
    public bool EnableScreenshots { get; set; }
    public bool EnableActivityTracking { get; set; }
    public bool EnableRealTimeReporting { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class MonitoringDataBatch
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime BatchStartTime { get; set; }
    public DateTime BatchEndTime { get; set; }
    public List<ApplicationUsage> Applications { get; set; } = new();
    public List<WebsiteVisit> Websites { get; set; } = new();
    public List<SystemEvent> SystemEvents { get; set; } = new();
    public List<ScreenshotCapture> Screenshots { get; set; } = new();
    public MonitoringStats Stats { get; set; } = new();
} 