using System.Diagnostics;
using System.Runtime.InteropServices;
using EmpAnalysis.Agent.Configuration;
using EmpAnalysis.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmpAnalysis.Agent.Services;

public interface IActivityMonitoringService
{
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    Task<MonitoringStats> GetCurrentStatsAsync();
    event EventHandler<ApplicationUsage>? ApplicationChanged;
    event EventHandler<SystemEvent>? SystemEventOccurred;
}

public class ActivityMonitoringService : IActivityMonitoringService
{
    private readonly MonitoringSettings _settings;
    private readonly ILogger<ActivityMonitoringService> _logger;
    private readonly IActiveWindowService _activeWindowService;
    private readonly IProductivityAnalyzer _productivityAnalyzer;

    private ApplicationUsage? _currentApplication;
    private readonly List<ApplicationUsage> _applicationHistory = new();
    private readonly List<WebsiteVisit> _websiteHistory = new();
    private readonly List<SystemEvent> _systemEvents = new();
    private DateTime _sessionStart = DateTime.UtcNow;

    public event EventHandler<ApplicationUsage>? ApplicationChanged;
    public event EventHandler<SystemEvent>? SystemEventOccurred;

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public ActivityMonitoringService(
        IOptions<AgentSettings> settings,
        ILogger<ActivityMonitoringService> logger,
        IActiveWindowService activeWindowService,
        IProductivityAnalyzer productivityAnalyzer)
    {
        _settings = settings.Value.MonitoringSettings;
        _logger = logger;
        _activeWindowService = activeWindowService;
        _productivityAnalyzer = productivityAnalyzer;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting activity monitoring...");

        // Start system event monitoring
        _ = Task.Run(() => MonitorSystemEventsAsync(cancellationToken), cancellationToken);

        // Main monitoring loop
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await MonitorActiveApplicationAsync();
                await Task.Delay(TimeSpan.FromSeconds(_settings.ActivityTrackingIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in activity monitoring loop");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        // End current application session
        if (_currentApplication != null)
        {
            _currentApplication.EndTime = DateTime.UtcNow;
            _applicationHistory.Add(_currentApplication);
        }

        _logger.LogInformation("Activity monitoring stopped");
    }

    private async Task MonitorActiveApplicationAsync()
    {
        var activeWindow = await _activeWindowService.GetActiveWindowInfoAsync();
        if (activeWindow == null)
            return;

        var now = DateTime.UtcNow;
        var applicationName = activeWindow.ApplicationName;
        var windowTitle = activeWindow.WindowTitle;

        // Check if this is a new application or window
        if (_currentApplication == null ||
            _currentApplication.ApplicationName != applicationName ||
            _currentApplication.WindowTitle != windowTitle)
        {
            // End previous application session
            if (_currentApplication != null)
            {
                _currentApplication.EndTime = now;
                _applicationHistory.Add(_currentApplication);
                _logger.LogDebug($"Application session ended: {_currentApplication.ApplicationName} ({_currentApplication.Duration})");
            }

            // Start new application session
            _currentApplication = new ApplicationUsage
            {
                ApplicationName = applicationName,
                ApplicationPath = GetApplicationPath(activeWindow.ProcessId),
                WindowTitle = windowTitle,
                StartTime = now,
                EndTime = now,
                IsProductiveApp = _productivityAnalyzer.IsProductiveApplication(applicationName),
                Category = _productivityAnalyzer.GetApplicationCategory(applicationName)
            };

            ApplicationChanged?.Invoke(this, _currentApplication);
            _logger.LogDebug($"New application detected: {applicationName} - {windowTitle}");

            // Check for website if it's a browser
            if (_settings.WebsiteTrackingEnabled && IsBrowser(applicationName))
            {
                await MonitorWebsiteAsync(windowTitle, now);
            }
        }
        else
        {
            // Update end time for current application
            _currentApplication.EndTime = now;
        }
    }

    private async Task MonitorWebsiteAsync(string windowTitle, DateTime timestamp)
    {
        try
        {
            var url = ExtractUrlFromBrowserTitle(windowTitle);
            if (string.IsNullOrEmpty(url))
                return;

            var domain = GetDomainFromUrl(url);
            var websiteVisit = new WebsiteVisit
            {
                Url = url,
                Title = windowTitle,
                Domain = domain,
                StartTime = timestamp,
                EndTime = timestamp,
                IsProductiveSite = _productivityAnalyzer.IsProductiveWebsite(domain),
                Category = _productivityAnalyzer.GetWebsiteCategory(domain)
            };

            _websiteHistory.Add(websiteVisit);
            _logger.LogDebug($"Website visit recorded: {domain}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring website activity");
        }
    }

    private async Task MonitorSystemEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Monitor idle time
            var lastActiveTime = DateTime.UtcNow;
            const int idleThresholdMinutes = 5;

            while (!cancellationToken.IsCancellationRequested)
            {
                var idleTime = GetIdleTime();
                var now = DateTime.UtcNow;

                if (idleTime.TotalMinutes >= idleThresholdMinutes)
                {
                    // User is idle
                    if (now.Subtract(lastActiveTime).TotalMinutes >= idleThresholdMinutes)
                    {
                        var systemEvent = new SystemEvent
                        {
                            Timestamp = now,
                            EventType = SystemEventType.Idle,
                            Description = "User idle detected",
                            Details = $"Idle for {idleTime.TotalMinutes:F1} minutes"
                        };

                        _systemEvents.Add(systemEvent);
                        SystemEventOccurred?.Invoke(this, systemEvent);
                        _logger.LogDebug($"User idle: {idleTime.TotalMinutes:F1} minutes");
                    }
                }
                else
                {
                    lastActiveTime = now;
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in system event monitoring");
        }
    }

    public async Task<MonitoringStats> GetCurrentStatsAsync()
    {
        var today = DateTime.Today;
        var todaysApplications = _applicationHistory
            .Where(a => a.StartTime.Date == today)
            .ToList();

        var todaysWebsites = _websiteHistory
            .Where(w => w.StartTime.Date == today)
            .ToList();

        var totalActiveTime = todaysApplications.Sum(a => a.Duration.TotalSeconds);
        var productiveTime = todaysApplications.Where(a => a.IsProductiveApp).Sum(a => a.Duration.TotalSeconds);
        var unproductiveTime = totalActiveTime - productiveTime;

        var productivityScore = totalActiveTime > 0 ? (productiveTime / totalActiveTime) * 100 : 0;

        var stats = new MonitoringStats
        {
            Date = today,
            TotalActiveTime = TimeSpan.FromSeconds(totalActiveTime),
            ProductiveTime = TimeSpan.FromSeconds(productiveTime),
            UnproductiveTime = TimeSpan.FromSeconds(unproductiveTime),
            ApplicationSwitches = todaysApplications.Count,
            WebsiteVisits = todaysWebsites.Count,
            ScreenshotsTaken = 0, // This would be set by the screenshot service
            ProductivityScore = productivityScore,
            TopApplications = todaysApplications
                .GroupBy(a => a.ApplicationName)
                .OrderByDescending(g => g.Sum(a => a.Duration.TotalSeconds))
                .Take(5)
                .Select(g => g.Key)
                .ToList(),
            TopWebsites = todaysWebsites
                .GroupBy(w => w.Domain)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList()
        };

        return stats;
    }

    private static TimeSpan GetIdleTime()
    {
        var lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        GetLastInputInfo(ref lastInputInfo);

        var idleTime = Environment.TickCount - lastInputInfo.dwTime;
        return TimeSpan.FromMilliseconds(idleTime);
    }

    private static string GetApplicationPath(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsBrowser(string applicationName)
    {
        var browsers = new[] { "chrome", "firefox", "edge", "safari", "opera", "brave", "vivaldi" };
        return browsers.Any(browser => applicationName.ToLowerInvariant().Contains(browser));
    }

    private static string ExtractUrlFromBrowserTitle(string windowTitle)
    {
        // This is a simplified URL extraction - in practice, you might need more sophisticated methods
        if (windowTitle.Contains(" - "))
        {
            var parts = windowTitle.Split(" - ");
            var lastPart = parts.LastOrDefault();
            if (lastPart != null && (lastPart.Contains("http") || lastPart.Contains("www")))
            {
                return lastPart;
            }
        }
        return string.Empty;
    }

    private static string GetDomainFromUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http"))
                url = "https://" + url;

            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }
}

public interface IProductivityAnalyzer
{
    bool IsProductiveApplication(string applicationName);
    bool IsProductiveWebsite(string domain);
    string GetApplicationCategory(string applicationName);
    string GetWebsiteCategory(string domain);
}

public class ProductivityAnalyzer : IProductivityAnalyzer
{
    private readonly Dictionary<string, bool> _productiveApps = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Microsoft Word", true },
        { "Microsoft Excel", true },
        { "Microsoft PowerPoint", true },
        { "Visual Studio", true },
        { "Visual Studio Code", true },
        { "Notepad++", true },
        { "Adobe Photoshop", true },
        { "Slack", true },
        { "Microsoft Teams", true },
        { "Outlook", true },
        { "Calculator", true },
        { "Steam", false },
        { "Discord", false },
        { "Spotify", false },
        { "VLC Media Player", false }
    };

    private readonly Dictionary<string, bool> _productiveWebsites = new(StringComparer.OrdinalIgnoreCase)
    {
        { "github.com", true },
        { "stackoverflow.com", true },
        { "microsoft.com", true },
        { "google.com", true },
        { "linkedin.com", true },
        { "facebook.com", false },
        { "twitter.com", false },
        { "youtube.com", false },
        { "instagram.com", false },
        { "reddit.com", false }
    };

    public bool IsProductiveApplication(string applicationName)
    {
        if (_productiveApps.TryGetValue(applicationName, out var isProductive))
            return isProductive;

        // Default logic for unknown applications
        var productiveKeywords = new[] { "office", "visual", "development", "work", "business", "productivity" };
        var unproductiveKeywords = new[] { "game", "media", "player", "social", "entertainment" };

        var appLower = applicationName.ToLowerInvariant();
        
        if (productiveKeywords.Any(keyword => appLower.Contains(keyword)))
            return true;
        
        if (unproductiveKeywords.Any(keyword => appLower.Contains(keyword)))
            return false;

        return true; // Default to productive for unknown apps
    }

    public bool IsProductiveWebsite(string domain)
    {
        if (_productiveWebsites.TryGetValue(domain, out var isProductive))
            return isProductive;

        // Default logic for unknown websites
        var productiveDomains = new[] { ".edu", ".gov", "docs.", "wiki", "learn", "tutorial", "documentation" };
        var unproductiveDomains = new[] { "social", "entertainment", "gaming", "streaming", "chat" };

        var domainLower = domain.ToLowerInvariant();
        
        if (productiveDomains.Any(keyword => domainLower.Contains(keyword)))
            return true;
        
        if (unproductiveDomains.Any(keyword => domainLower.Contains(keyword)))
            return false;

        return true; // Default to productive for unknown sites
    }

    public string GetApplicationCategory(string applicationName)
    {
        var appLower = applicationName.ToLowerInvariant();
        
        if (appLower.Contains("visual") || appLower.Contains("code") || appLower.Contains("development"))
            return "Development";
        
        if (appLower.Contains("office") || appLower.Contains("word") || appLower.Contains("excel") || appLower.Contains("powerpoint"))
            return "Office";
        
        if (appLower.Contains("browser") || appLower.Contains("chrome") || appLower.Contains("firefox") || appLower.Contains("edge"))
            return "Web Browser";
        
        if (appLower.Contains("media") || appLower.Contains("player") || appLower.Contains("music"))
            return "Media";
        
        if (appLower.Contains("game") || appLower.Contains("steam"))
            return "Gaming";
        
        if (appLower.Contains("chat") || appLower.Contains("teams") || appLower.Contains("slack") || appLower.Contains("discord"))
            return "Communication";
        
        return "Other";
    }

    public string GetWebsiteCategory(string domain)
    {
        var domainLower = domain.ToLowerInvariant();
        
        if (domainLower.Contains("github") || domainLower.Contains("stackoverflow") || domainLower.Contains("docs"))
            return "Development";
        
        if (domainLower.Contains("google") || domainLower.Contains("search") || domainLower.Contains("bing"))
            return "Search";
        
        if (domainLower.Contains("social") || domainLower.Contains("facebook") || domainLower.Contains("twitter") || domainLower.Contains("linkedin"))
            return "Social Media";
        
        if (domainLower.Contains("youtube") || domainLower.Contains("streaming") || domainLower.Contains("video"))
            return "Video/Streaming";
        
        if (domainLower.Contains("news") || domainLower.Contains("blog"))
            return "News/Blog";
        
        return "Other";
    }
} 