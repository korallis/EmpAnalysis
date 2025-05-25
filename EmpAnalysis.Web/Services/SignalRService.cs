using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using System.Net.Http;

namespace EmpAnalysis.Web.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _hubConnection;
    private readonly string _hubUrl;
    private readonly IWebHostEnvironment _environment;

    public SignalRService(ILogger<SignalRService> logger, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        // Use the API base URL for SignalR hub
        var apiUrl = configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7001";
        _hubUrl = $"{apiUrl}/hubs/monitoring";
    }

    public async Task StartAsync()
    {
        try
        {
            var hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.SkipNegotiation = false;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                    
                    // For development, bypass SSL certificate validation
                    if (_environment.IsDevelopment())
                    {
                        options.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                        };
                    }
                })
                .WithAutomaticReconnect();

            _hubConnection = hubConnectionBuilder.Build();

            // Subscribe to dashboard updates
            _hubConnection.On<object>("DashboardUpdate", OnDashboardUpdate);
            _hubConnection.On<object>("ActivityUpdate", OnActivityUpdate);
            _hubConnection.On<object>("EmployeeStatusUpdate", OnEmployeeStatusUpdate);
            _hubConnection.On<object>("ScreenshotUpdate", OnScreenshotUpdate);
            _hubConnection.On<object>("SystemAlert", OnSystemAlert);

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinDashboardGroup");

            _logger.LogInformation("SignalR connection established to {HubUrl}", _hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SignalR connection to {HubUrl}", _hubUrl);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.InvokeAsync("LeaveDashboardGroup");
                await _hubConnection.StopAsync();
                _logger.LogInformation("SignalR connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing SignalR connection");
            }
        }
    }

    // Events for real-time updates
    public event Action<object>? DashboardUpdated;
    public event Action<object>? ActivityUpdated;
    public event Action<object>? EmployeeStatusUpdated;
    public event Action<object>? ScreenshotUpdated;
    public event Action<object>? SystemAlertReceived;

    private void OnDashboardUpdate(object data)
    {
        _logger.LogDebug("Received dashboard update: {Data}", JsonSerializer.Serialize(data));
        DashboardUpdated?.Invoke(data);
    }

    private void OnActivityUpdate(object data)
    {
        _logger.LogDebug("Received activity update: {Data}", JsonSerializer.Serialize(data));
        ActivityUpdated?.Invoke(data);
    }

    private void OnEmployeeStatusUpdate(object data)
    {
        _logger.LogDebug("Received employee status update: {Data}", JsonSerializer.Serialize(data));
        EmployeeStatusUpdated?.Invoke(data);
    }

    private void OnScreenshotUpdate(object data)
    {
        _logger.LogDebug("Received screenshot update: {Data}", JsonSerializer.Serialize(data));
        ScreenshotUpdated?.Invoke(data);
    }

    private void OnSystemAlert(object data)
    {
        _logger.LogDebug("Received system alert: {Data}", JsonSerializer.Serialize(data));
        SystemAlertReceived?.Invoke(data);
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }
} 