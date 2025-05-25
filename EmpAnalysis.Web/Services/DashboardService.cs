using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Web.Services;

public class DashboardService
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(EmpAnalysisDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var last5Minutes = DateTime.UtcNow.AddMinutes(-5);
            var lastHour = DateTime.UtcNow.AddHours(-1);

            // Get all data for better performance
            var totalEmployees = await GetTotalEmployeesAsync();
            var productiveHours = await GetProductiveHoursAsync(thisWeek, today.AddDays(1));
            var totalScreenshots = await GetTotalScreenshotsAsync();
            var securityAlerts = await GetSecurityAlertsAsync();
            var productivityScore = await GetProductivityScoreAsync();
            var networkEvents = await GetNetworkEventsAsync();
            var employeeStatusCounts = await GetEmployeeStatusCountsAsync(last5Minutes, lastHour);
            var recentActivities = await GetRecentActivitiesDataAsync();
            var topApplications = await GetTopApplicationsDataAsync();
            var recentAlerts = await GetRecentAlertsDataAsync();
            
            // Fetch analytics from API if available
            using var httpClient = new HttpClient();
            var apiUrl = "/api/monitoring/dashboard";
            var response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("analytics", out var analyticsElem))
                {
                    var analytics = System.Text.Json.JsonSerializer.Deserialize<AdvancedAnalyticsResult>(analyticsElem.GetRawText());
                    return new DashboardData
                    {
                        TotalEmployees = totalEmployees,
                        TotalProductiveHours = productiveHours,
                        TotalScreenshots = totalScreenshots,
                        SecurityAlerts = securityAlerts,
                        ProductivityScore = productivityScore,
                        NetworkEvents = networkEvents,
                        OnlineEmployees = employeeStatusCounts.Online,
                        IdleEmployees = employeeStatusCounts.Idle,
                        OfflineEmployees = employeeStatusCounts.Offline,
                        RecentActivities = recentActivities,
                        TopApplications = topApplications,
                        RecentAlerts = recentAlerts,
                        Analytics = analytics
                    };
                }
            }
            // fallback: return local data
            return new DashboardData
            {
                TotalEmployees = totalEmployees,
                TotalProductiveHours = productiveHours,
                TotalScreenshots = totalScreenshots,
                SecurityAlerts = securityAlerts,
                ProductivityScore = productivityScore,
                NetworkEvents = networkEvents,
                OnlineEmployees = employeeStatusCounts.Online,
                IdleEmployees = employeeStatusCounts.Idle,
                OfflineEmployees = employeeStatusCounts.Offline,
                RecentActivities = recentActivities,
                TopApplications = topApplications,
                RecentAlerts = recentAlerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return new DashboardData(); // Return empty data on error
        }
    }

    private async Task<int> GetTotalEmployeesAsync()
    {
        return await _context.Users.Where(e => e.IsActive).CountAsync();
    }

    private async Task<double> GetProductiveHoursAsync(DateTime startDate, DateTime endDate)
    {
        var productiveApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= startDate && a.StartTime < endDate && a.IsProductiveApplication)
            .ToListAsync();

        return productiveApps.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value.TotalHours);
    }

    private async Task<int> GetTotalScreenshotsAsync()
    {
        return await _context.Screenshots.Where(s => s.CapturedAt >= DateTime.Today).CountAsync();
    }

    private async Task<int> GetSecurityAlertsAsync()
    {
        return await _context.ActivityLogs
            .Where(a => a.Timestamp >= DateTime.Today && 
                       (a.ActivityType == ActivityType.UsbDevice || 
                        a.ActivityType == ActivityType.SystemEvent))
            .CountAsync();
    }

    private async Task<double> GetProductivityScoreAsync()
    {
        var allApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= DateTime.Today)
            .ToListAsync();

        if (!allApps.Any()) return 0.0;

        var totalHours = allApps.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value.TotalHours);
        var productiveHours = allApps.Where(a => a.IsProductiveApplication && a.Duration.HasValue)
            .Sum(a => a.Duration!.Value.TotalHours);

        return totalHours > 0 ? (productiveHours / totalHours) * 100 : 0.0;
    }

    private async Task<int> GetNetworkEventsAsync()
    {
        return await _context.ActivityLogs
            .Where(a => a.Timestamp >= DateTime.Today && a.ActivityType == ActivityType.SystemEvent)
            .CountAsync();
    }

    private async Task<EmployeeStatusCounts> GetEmployeeStatusCountsAsync(DateTime last5Minutes, DateTime lastHour)
    {
        var employees = await _context.Users
            .Where(e => e.IsActive)
            .Select(e => new { e.LastLoginAt })
            .ToListAsync();

        var online = employees.Count(e => e.LastLoginAt >= last5Minutes);
        var idle = employees.Count(e => e.LastLoginAt >= lastHour && e.LastLoginAt < last5Minutes);
        var offline = employees.Count(e => e.LastLoginAt < lastHour);

        return new EmployeeStatusCounts { Online = online, Idle = idle, Offline = offline };
    }

    private async Task<List<ActivityData>> GetRecentActivitiesDataAsync()
    {
        return await _context.ActivityLogs
            .Include(a => a.Employee)
            .Where(a => a.Timestamp >= DateTime.Today)
            .OrderByDescending(a => a.Timestamp)
            .Take(15)
            .Select(a => new ActivityData
            {
                Id = a.Id,
                EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                Description = a.Description,
                Timestamp = a.Timestamp,
                ActivityType = a.ActivityType
            })
            .ToListAsync();
    }

    private async Task<List<ApplicationUsageData>> GetTopApplicationsDataAsync()
    {
        var todayApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= DateTime.Today)
            .ToListAsync();

        return todayApps
            .GroupBy(a => a.ApplicationName)
            .Select(g => new
            {
                ApplicationName = g.Key,
                TotalMinutes = g.Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0),
                UsageCount = g.Count()
            })
            .OrderByDescending(x => x.TotalMinutes)
            .Take(8)
            .ToList();
    }

    private async Task<List<AlertData>> GetRecentAlertsDataAsync()
    {
        return await _context.ActivityLogs
            .Include(a => a.Employee)
            .Where(a => a.Timestamp >= DateTime.Today.AddDays(-7))
            .Where(a => a.ActivityType == ActivityType.UsbDevice || 
                       a.ActivityType == ActivityType.SystemEvent)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .Select(a => new AlertData
            {
                Id = a.Id,
                Description = a.Description,
                Timestamp = a.Timestamp,
                EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                ActivityType = a.ActivityType
            })
            .ToListAsync();
    }

    public async Task<DashboardMetrics> GetDashboardMetricsAsync()
    {
        var today = DateTime.Today;
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);

        var activeEmployeeCount = await _context.Users
            .Where(e => e.IsActive && e.LastLoginAt >= thisWeek)
            .CountAsync();

        var todayScreenshots = await _context.Screenshots
            .Where(s => s.CapturedAt >= today)
            .CountAsync();

        var weeklyProductiveHours = await CalculateProductiveHoursAsync(thisWeek, today.AddDays(1));

        var riskAlerts = await _context.ActivityLogs
            .Where(a => a.Timestamp >= today && 
                       (a.ActivityType == ActivityType.SystemEvent || 
                        a.ActivityType == ActivityType.UsbDevice))
            .CountAsync();

        var productivityScore = await CalculateProductivityScoreAsync();

        var networkEvents = await _context.ActivityLogs
            .Where(a => a.Timestamp >= today && a.ActivityType == ActivityType.SystemEvent)
            .CountAsync();

        return new DashboardMetrics
        {
            ActiveEmployees = activeEmployeeCount,
            ProductiveHours = weeklyProductiveHours,
            ScreenshotsToday = todayScreenshots,
            RiskAlerts = riskAlerts,
            ProductivityScore = productivityScore,
            NetworkEvents = networkEvents
        };
    }

    public async Task<List<EmployeeStatus>> GetEmployeeStatusesAsync()
    {
        var lastHour = DateTime.UtcNow.AddHours(-1);
        var last5Minutes = DateTime.UtcNow.AddMinutes(-5);

        var employees = await _context.Users
            .Where(e => e.IsActive)
            .Select(e => new EmployeeStatus
            {
                Id = e.Id,
                Name = $"{e.FirstName} {e.LastName}",
                Department = e.Department ?? "Unknown",
                Status = e.LastLoginAt >= last5Minutes ? "Online" :
                         e.LastLoginAt >= lastHour ? "Idle" : "Offline",
                LastActivity = e.LastLoginAt ?? DateTime.MinValue,
                ProductivityToday = 85 // TODO: Calculate from actual data
            })
            .OrderByDescending(e => e.LastActivity)
            .Take(20)
            .ToListAsync();

        return employees;
    }

    public async Task<List<ActivityItem>> GetRecentActivitiesAsync()
    {
        var activities = await _context.ActivityLogs
            .Include(a => a.Employee)
            .Where(a => a.Timestamp >= DateTime.Today)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new ActivityItem
            {
                Id = a.Id,
                EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                Activity = a.Description,
                Timestamp = a.Timestamp,
                Type = GetActivityDisplayType(a.ActivityType)
            })
            .ToListAsync();

        return activities;
    }

    public async Task<List<ApplicationUsage>> GetTopApplicationsAsync()
    {
        var today = DateTime.Today;
        var todayApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= today)
            .ToListAsync();

        var topApps = todayApps
            .GroupBy(a => a.ApplicationName)
            .Select(g => new
            {
                ApplicationName = g.Key,
                TotalMinutes = g.Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0),
                UsageCount = g.Count()
            })
            .OrderByDescending(x => x.TotalMinutes)
            .Take(10)
            .ToList();

        var totalMinutes = topApps.Sum(a => a.TotalMinutes);
        
        return topApps.Select(app => new ApplicationUsage
        {
            ApplicationName = app.ApplicationName,
            UsagePercentage = totalMinutes > 0 ? (app.TotalMinutes / totalMinutes) * 100 : 0,
            TotalMinutes = app.TotalMinutes,
            UsageCount = app.UsageCount
        }).ToList();
    }

    public async Task<List<AlertItem>> GetRecentAlertsAsync()
    {
        var recentAlerts = await _context.ActivityLogs
            .Include(a => a.Employee)
            .Where(a => a.Timestamp >= DateTime.Today.AddDays(-7))
            .Where(a => a.ActivityType == ActivityType.SystemEvent || 
                       a.ActivityType == ActivityType.UsbDevice)
            .OrderByDescending(a => a.Timestamp)
            .Take(15)
            .Select(a => new AlertItem
            {
                Id = a.Id,
                Title = GetAlertTitle(a.ActivityType, a.Description),
                Message = a.Description,
                Severity = GetAlertSeverity(a.ActivityType),
                Timestamp = a.Timestamp,
                EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}"
            })
            .ToListAsync();

        return recentAlerts;
    }

    public async Task<SystemHealth> GetSystemHealthAsync()
    {
        var totalAgents = await _context.Users.Where(e => e.IsActive).CountAsync();
        var activeAgents = await _context.Users
            .Where(e => e.IsActive && e.LastLoginAt >= DateTime.UtcNow.AddMinutes(-10))
            .CountAsync();

        var todayScreenshots = await _context.Screenshots
            .Where(s => s.CapturedAt >= DateTime.Today)
            .CountAsync();

        var avgFileSize = await _context.Screenshots
            .Where(s => s.CapturedAt >= DateTime.Today)
            .AverageAsync(s => (double?)s.FileSizeBytes) ?? 0;

        var storageUsedMB = await _context.Screenshots
            .Where(s => s.CapturedAt >= DateTime.Today.AddDays(-30))
            .SumAsync(s => (long?)s.FileSizeBytes) ?? 0;

        return new SystemHealth
        {
            TotalAgents = totalAgents,
            ActiveAgents = activeAgents,
            AgentStatus = $"{activeAgents}/{totalAgents} Online",
            ScreenshotCount = todayScreenshots,
            AvgScreenshotSize = $"{avgFileSize / 1024:F1} KB",
            StorageUsed = $"{storageUsedMB / (1024 * 1024):F1} MB",
            SystemUptime = "99.8%"
        };
    }

    public async Task<ProductivityData> GetProductivityDataAsync()
    {
        var today = DateTime.Today;
        var todayApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= today)
            .ToListAsync();

        var hourlyData = new List<HourlyProductivity>();

        for (int hour = 0; hour < 24; hour++)
        {
            var hourStart = today.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);

            var hourApps = todayApps.Where(a => a.StartTime >= hourStart && a.StartTime < hourEnd).ToList();

            var productiveMinutes = hourApps
                .Where(a => a.IsProductiveApplication && a.Duration.HasValue)
                .Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);

            var totalMinutes = hourApps
                .Where(a => a.Duration.HasValue)
                .Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);

            hourlyData.Add(new HourlyProductivity
            {
                Hour = hour,
                ProductivityPercentage = totalMinutes > 0 ? (productiveMinutes / totalMinutes) * 100 : 0,
                ActiveUsers = hourApps.Select(a => a.EmployeeId).Distinct().Count()
            });
        }

        return new ProductivityData
        {
            HourlyData = hourlyData,
            AverageProductivity = hourlyData.Any() ? hourlyData.Average(h => h.ProductivityPercentage) : 0
        };
    }

    private async Task<int> CalculateProductiveHoursAsync(DateTime startDate, DateTime endDate)
    {
        var productiveApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= startDate && a.StartTime < endDate && a.IsProductiveApplication)
            .ToListAsync();

        var productiveMinutes = productiveApps
            .Where(a => a.Duration.HasValue)
            .Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);

        return (int)(productiveMinutes / 60);
    }

    private async Task<int> CalculateProductivityScoreAsync()
    {
        var today = DateTime.Today;
        
        var allApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= today)
            .ToListAsync();

        var productiveMinutes = allApps
            .Where(a => a.IsProductiveApplication && a.Duration.HasValue)
            .Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);

        var totalMinutes = allApps
            .Where(a => a.Duration.HasValue)
            .Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);

        return totalMinutes > 0 ? (int)((productiveMinutes / totalMinutes) * 100) : 0;
    }

    private static string GetActivityDisplayType(ActivityType activityType) => activityType switch
    {
        ActivityType.Login => "Login",
        ActivityType.Logout => "Logout",
        ActivityType.ScreenCapture => "Screenshot",
        ActivityType.ApplicationStart => "App Launch",
        ActivityType.WebsiteVisit => "Web Browsing",
        ActivityType.FileAccess => "File Operation",
        ActivityType.SystemEvent => "System Event",
        ActivityType.UsbDevice => "USB Device",
        ActivityType.EmailActivity => "Email Activity",
        _ => "Unknown"
    };

    private static string GetAlertTitle(ActivityType activityType, string description) => activityType switch
    {
        ActivityType.UsbDevice => "USB Device Alert",
        ActivityType.SystemEvent => "System Event",
        ActivityType.EmailActivity => "Email Activity",
        _ => "System Notification"
    };

    private static string GetAlertSeverity(ActivityType activityType) => activityType switch
    {
        ActivityType.UsbDevice => "critical",
        ActivityType.SystemEvent => "warning",
        ActivityType.EmailActivity => "info",
        _ => "info"
    };
}

// Data models for dashboard
public class DashboardMetrics
{
    public int ActiveEmployees { get; set; }
    public int ProductiveHours { get; set; }
    public int ScreenshotsToday { get; set; }
    public int RiskAlerts { get; set; }
    public int ProductivityScore { get; set; }
    public int NetworkEvents { get; set; }
}

public class EmployeeStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public int ProductivityToday { get; set; }
}

public class ActivityItem
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class ApplicationUsage
{
    public string ApplicationName { get; set; } = string.Empty;
    public double UsagePercentage { get; set; }
    public double TotalMinutes { get; set; }
    public int UsageCount { get; set; }
}

public class AlertItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
}

public class SystemHealth
{
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public string AgentStatus { get; set; } = string.Empty;
    public int ScreenshotCount { get; set; }
    public string AvgScreenshotSize { get; set; } = string.Empty;
    public string StorageUsed { get; set; } = string.Empty;
    public string SystemUptime { get; set; } = string.Empty;
}

public class ProductivityData
{
    public List<HourlyProductivity> HourlyData { get; set; } = new();
    public double AverageProductivity { get; set; }
}

public class HourlyProductivity
{
    public int Hour { get; set; }
    public double ProductivityPercentage { get; set; }
    public int ActiveUsers { get; set; }
}

// Enhanced Dashboard Data Models
public class DashboardData
{
    public int TotalEmployees { get; set; }
    public double TotalProductiveHours { get; set; }
    public int TotalScreenshots { get; set; }
    public int SecurityAlerts { get; set; }
    public double ProductivityScore { get; set; }
    public int NetworkEvents { get; set; }
    public int OnlineEmployees { get; set; }
    public int IdleEmployees { get; set; }
    public int OfflineEmployees { get; set; }
    public List<ActivityData> RecentActivities { get; set; } = new();
    public List<ApplicationUsageData> TopApplications { get; set; } = new();
    public List<AlertData> RecentAlerts { get; set; } = new();
    public AdvancedAnalyticsResult? Analytics { get; set; }
}

public class ActivityData
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ActivityType ActivityType { get; set; }
}

public class ApplicationUsageData
{
    public string ApplicationName { get; set; } = string.Empty;
    public double TotalHours { get; set; }
    public int UsageCount { get; set; }
}

public class AlertData
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
}

public class EmployeeStatusCounts
{
    public int Online { get; set; }
    public int Idle { get; set; }
    public int Offline { get; set; }
}

public class AdvancedAnalyticsResult
{
    public List<AnalyticsData> Data { get; set; } = new();
    public string Timeframe { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class AnalyticsData
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}