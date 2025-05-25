using EmpAnalysis.Agent.Configuration;
using EmpAnalysis.Agent.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Text;

namespace EmpAnalysis.Agent.Services;

public class MonitoringCoordinator : BackgroundService
{
    private readonly ILogger<MonitoringCoordinator> _logger;
    private readonly AgentSettings _settings;
    private readonly IActivityMonitoringService _activityService;
    private readonly IScreenshotService _screenshotService;
    private readonly IApiCommunicationService _apiService;

    private readonly List<ApplicationUsage> _pendingApplications = new();
    private readonly List<WebsiteVisit> _pendingWebsites = new();
    private readonly List<SystemEvent> _pendingSystemEvents = new();
    private readonly List<ScreenshotCapture> _pendingScreenshots = new();

    private DateTime _lastDataSync = DateTime.UtcNow;
    private DateTime _lastScreenshot = DateTime.UtcNow;
    private string? _agentId;

    // Enhanced monitoring fields
    private DateTime _lastActivityTime = DateTime.UtcNow;
    private bool _isIdle = false;
    private string _lastWindowTitle = string.Empty;
    private string _lastActiveApplication = string.Empty;

    // Windows API imports for enhanced monitoring
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public MonitoringCoordinator(
        ILogger<MonitoringCoordinator> logger,
        IOptions<AgentSettings> settings,
        IActivityMonitoringService activityService,
        IScreenshotService screenshotService,
        IApiCommunicationService apiService)
    {
        _logger = logger;
        _settings = settings.Value;
        _activityService = activityService;
        _screenshotService = screenshotService;
        _apiService = apiService;

        // Subscribe to activity events
        _activityService.ApplicationChanged += OnApplicationChanged;
        _activityService.SystemEventOccurred += OnSystemEventOccurred;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enhanced Monitoring Coordinator starting...");

        // Initial setup
        await InitializeAgentAsync();        // Start enhanced monitoring tasks
        await Task.WhenAll(
            MonitorScreenshotsAsync(stoppingToken),
            SynchronizeDataAsync(stoppingToken),
            MonitorHeartbeatAsync(stoppingToken),
            MonitorWindowTitlesAsync(stoppingToken), // New: Window title monitoring
            MonitorIdleTimeAsync(stoppingToken)      // New: Idle time detection
        );
    }

    private async Task InitializeAgentAsync()
    {
        try
        {
            _logger.LogInformation("Initializing monitoring agent...");

            // Check API connection
            var isConnected = await _apiService.CheckConnectionAsync();
            if (!isConnected)
            {
                _logger.LogWarning("Unable to connect to API server. Agent will work in offline mode.");
            }
            else
            {
                // Register agent
                var employeeId = _settings.EmployeeSettings.AutoDetectUser
                    ? Environment.UserName
                    : _settings.EmployeeSettings.EmployeeId;

                var registrationResponse = await _apiService.RegisterAgentAsync(employeeId, Environment.MachineName);

                if (registrationResponse != null)
                {
                    _agentId = registrationResponse.AgentId;
                    _logger.LogInformation($"Agent registered successfully with ID: {_agentId}");
                }
                else
                {
                    _logger.LogWarning("Agent registration failed. Continuing in offline mode.");
                }
            }

            // Create local data directories
            CreateDataDirectories();

            _logger.LogInformation("Agent initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent initialization");
        }
    }

    private async Task MonitorScreenshotsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screenshot monitoring...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeSinceLastScreenshot = now - _lastScreenshot;

                if (timeSinceLastScreenshot.TotalSeconds >= _settings.MonitoringSettings.ScreenshotIntervalSeconds)
                {
                    // Check if it's working hours
                    if (IsWorkingTime(now))
                    {
                        var screenshot = await _screenshotService.CaptureScreenshotAsync();
                        if (screenshot != null)
                        {
                            lock (_pendingScreenshots)
                            {
                                _pendingScreenshots.Add(screenshot);
                            }

                            // Save locally
                            var screenshotDir = Path.Combine(GetDataDirectory(), "screenshots", now.ToString("yyyy-MM-dd"));
                            await _screenshotService.SaveScreenshotAsync(screenshot, screenshotDir);

                            _lastScreenshot = now;
                            _logger.LogDebug($"Screenshot captured and queued. Total pending: {_pendingScreenshots.Count}");
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in screenshot monitoring");
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }
    }

    private async Task SynchronizeDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data synchronization...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeSinceLastSync = now - _lastDataSync;

                // Sync every 5 minutes
                if (timeSinceLastSync.TotalMinutes >= 5)
                {
                    await PerformDataSyncAsync();
                    _lastDataSync = now;
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data synchronization");
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
            }
        }
    }

    private async Task PerformDataSyncAsync()
    {
        try
        {
            List<ApplicationUsage> applicationsToSync;
            List<WebsiteVisit> websitesToSync;
            List<SystemEvent> systemEventsToSync;
            List<ScreenshotCapture> screenshotsToSync;

            // Get pending data
            lock (_pendingApplications)
            {
                applicationsToSync = new List<ApplicationUsage>(_pendingApplications);
                _pendingApplications.Clear();
            }

            lock (_pendingWebsites)
            {
                websitesToSync = new List<WebsiteVisit>(_pendingWebsites);
                _pendingWebsites.Clear();
            }

            lock (_pendingSystemEvents)
            {
                systemEventsToSync = new List<SystemEvent>(_pendingSystemEvents);
                _pendingSystemEvents.Clear();
            }

            lock (_pendingScreenshots)
            {
                screenshotsToSync = new List<ScreenshotCapture>(_pendingScreenshots);
                _pendingScreenshots.Clear();
            }

            if (applicationsToSync.Count == 0 && websitesToSync.Count == 0 &&
                systemEventsToSync.Count == 0 && screenshotsToSync.Count == 0)
            {
                return; // No data to sync
            }

            _logger.LogDebug($"Syncing data: {applicationsToSync.Count} apps, {websitesToSync.Count} websites, " +
                           $"{systemEventsToSync.Count} events, {screenshotsToSync.Count} screenshots");

            // Send activity data
            if (applicationsToSync.Count > 0 || websitesToSync.Count > 0)
            {
                var activitySent = await _apiService.SendActivityDataAsync(applicationsToSync, websitesToSync);
                if (!activitySent)
                {
                    // Re-queue data if send failed
                    lock (_pendingApplications)
                    {
                        _pendingApplications.AddRange(applicationsToSync);
                    }
                    lock (_pendingWebsites)
                    {
                        _pendingWebsites.AddRange(websitesToSync);
                    }
                }
            }

            // Send system events
            if (systemEventsToSync.Count > 0)
            {
                var eventsSent = await _apiService.SendSystemEventsAsync(systemEventsToSync);
                if (!eventsSent)
                {
                    lock (_pendingSystemEvents)
                    {
                        _pendingSystemEvents.AddRange(systemEventsToSync);
                    }
                }
            }

            // Send screenshots (one by one to avoid large payloads)
            foreach (var screenshot in screenshotsToSync)
            {
                var screenshotSent = await _apiService.SendScreenshotAsync(screenshot);
                if (!screenshotSent)
                {
                    lock (_pendingScreenshots)
                    {
                        _pendingScreenshots.Add(screenshot);
                    }
                }

                // Small delay between screenshots
                await Task.Delay(100);
            }

            _logger.LogInformation($"Data synchronization completed. Remaining pending: " +
                                 $"{_pendingApplications.Count + _pendingWebsites.Count + _pendingSystemEvents.Count + _pendingScreenshots.Count} items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data synchronization");
        }
    }

    private async Task MonitorHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting health monitoring...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check memory usage
                var workingSet = Environment.WorkingSet;
                var workingSetMB = workingSet / (1024 * 1024);

                if (workingSetMB > 500) // Alert if using more than 500MB
                {
                    _logger.LogWarning($"High memory usage detected: {workingSetMB} MB");
                }

                // Check pending data queue sizes
                var totalPending = _pendingApplications.Count + _pendingWebsites.Count +
                                 _pendingSystemEvents.Count + _pendingScreenshots.Count;

                if (totalPending > 1000) // Alert if too much pending data
                {
                    _logger.LogWarning($"Large number of pending items: {totalPending}");
                }

                // Log stats periodically
                var stats = await _activityService.GetCurrentStatsAsync();
                _logger.LogInformation($"Daily stats - Active: {stats.TotalActiveTime:hh\\:mm\\:ss}, " +
                                     $"Productive: {stats.ProductiveTime:hh\\:mm\\:ss}, " +
                                     $"Score: {stats.ProductivityScore:F1}%, " +
                                     $"Apps: {stats.ApplicationSwitches}, " +
                                     $"Websites: {stats.WebsiteVisits}");

                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health monitoring");
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }

    private async Task MonitorHeartbeatAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting heartbeat monitoring...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.ApiSettings.HeartbeatIntervalSeconds), cancellationToken);

                if (!string.IsNullOrEmpty(_agentId))
                {
                    var employeeId = _settings.EmployeeSettings.AutoDetectUser
                        ? Environment.UserName
                        : _settings.EmployeeSettings.EmployeeId;

                    var success = await _apiService.SendHeartbeatAsync(_agentId, employeeId);

                    if (success)
                    {
                        _logger.LogDebug($"Heartbeat sent successfully for agent {_agentId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to send heartbeat for agent {_agentId}");
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping heartbeat - agent not registered");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat monitoring");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        _logger.LogInformation("Heartbeat monitoring stopped");
    }

    private void OnApplicationChanged(object? sender, ApplicationUsage application)
    {
        lock (_pendingApplications)
        {
            _pendingApplications.Add(application);
        }
    }

    private void OnSystemEventOccurred(object? sender, SystemEvent systemEvent)
    {
        lock (_pendingSystemEvents)
        {
            _pendingSystemEvents.Add(systemEvent);
        }
    }

    private bool IsWorkingTime(DateTime dateTime)
    {
        return _settings.EmployeeSettings.WorkingHours.IsWorkingTime(dateTime);
    }

    private void CreateDataDirectories()
    {
        var dataDir = GetDataDirectory();
        var screenshotsDir = Path.Combine(dataDir, "screenshots");
        var logsDir = Path.Combine(dataDir, "logs");

        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(screenshotsDir);
        Directory.CreateDirectory(logsDir);

        _logger.LogDebug($"Data directories created: {dataDir}");
    }

    private static string GetDataDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "EmpAnalysis", "Agent");
    }

    private async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("Performing cleanup...");

            // Final data sync
            await PerformDataSyncAsync();

            // Clean up old screenshots (keep only last 30 days)
            await CleanupOldFilesAsync();

            _logger.LogInformation("Cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }

    private async Task CleanupOldFilesAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var screenshotsDir = Path.Combine(GetDataDirectory(), "screenshots");
                if (!Directory.Exists(screenshotsDir))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-30);
                var directories = Directory.GetDirectories(screenshotsDir);

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    if (DateTime.TryParseExact(dirName, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var dirDate))
                    {
                        if (dirDate < cutoffDate)
                        {
                            Directory.Delete(dir, true);
                            _logger.LogDebug($"Deleted old screenshot directory: {dirName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old files");
            }
        });
    }

    public override void Dispose()
    {
        _activityService.ApplicationChanged -= OnApplicationChanged;
        _activityService.SystemEventOccurred -= OnSystemEventOccurred;
        base.Dispose();
    }

    // New: Enhanced window title monitoring
    private async Task MonitorWindowTitlesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting window title monitoring...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var windowInfo = GetActiveWindowInfo();

                if (!string.IsNullOrEmpty(windowInfo.Title) &&
                    windowInfo.Title != _lastWindowTitle)
                {
                    _logger.LogDebug($"Window changed: {windowInfo.ProcessName} - {windowInfo.Title}");

                    // Record window change activity
                    var windowActivity = new WindowActivity
                    {
                        Timestamp = DateTime.UtcNow,
                        WindowTitle = windowInfo.Title,
                        ProcessName = windowInfo.ProcessName,
                        Duration = TimeSpan.Zero // Will be calculated on next change
                    };

                    // Update last activity time
                    _lastActivityTime = DateTime.UtcNow;
                    _isIdle = false;

                    _lastWindowTitle = windowInfo.Title;
                    _lastActiveApplication = windowInfo.ProcessName;                    // Add to pending activities for batch submission
                    lock (_pendingSystemEvents)
                    {
                        _pendingSystemEvents.Add(new SystemEvent
                        {
                            Timestamp = windowActivity.Timestamp,
                            EventType = SystemEventType.WindowChange,
                            Description = "Window changed",
                            Details = $"{windowActivity.ProcessName}: {windowActivity.WindowTitle}"
                        });
                    }
                }

                await Task.Delay(1000, cancellationToken); // Check every second
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in window title monitoring");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    // New: Idle time detection
    private async Task MonitorIdleTimeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting idle time monitoring...");
        const int idleThresholdMinutes = 5; // Configurable threshold

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var timeSinceLastActivity = DateTime.UtcNow - _lastActivityTime;
                var isCurrentlyIdle = timeSinceLastActivity.TotalMinutes >= idleThresholdMinutes;                if (isCurrentlyIdle != _isIdle)
                {
                    _isIdle = isCurrentlyIdle;
                    var eventType = _isIdle ? SystemEventType.IdleStart : SystemEventType.IdleEnd;
                    var description = _isIdle ? "User became idle" : "User became active";
                    
                    _logger.LogInformation($"{description}: {timeSinceLastActivity.TotalMinutes:F1} minutes since last activity");
                    
                    lock (_pendingSystemEvents)
                    {
                        _pendingSystemEvents.Add(new SystemEvent
                        {
                            Timestamp = DateTime.UtcNow,
                            EventType = eventType,
                            Description = description,
                            Details = $"Idle duration: {timeSinceLastActivity.TotalMinutes:F1} minutes"
                        });
                    }
                }

                await Task.Delay(30000, cancellationToken); // Check every 30 seconds
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in idle time monitoring");
                await Task.Delay(30000, cancellationToken);
            }
        }
    }

    // Helper method to get active window information
    private (string Title, string ProcessName) GetActiveWindowInfo()
    {
        try
        {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
                return (string.Empty, string.Empty);

            // Get window title
            int length = GetWindowTextLength(handle);
            StringBuilder title = new StringBuilder(length + 1);
            GetWindowText(handle, title, title.Capacity);

            // Get process information
            GetWindowThreadProcessId(handle, out uint processId);
            var process = System.Diagnostics.Process.GetProcessById((int)processId);

            return (title.ToString(), process.ProcessName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active window info");
            return (string.Empty, string.Empty);
        }
    }
}