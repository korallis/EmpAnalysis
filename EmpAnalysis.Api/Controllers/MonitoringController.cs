using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;
using EmpAnalysis.Api.Hubs;
using System.Security.Claims;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<MonitoringController> _logger;
    private readonly IHubContext<MonitoringHub> _hubContext;

    public MonitoringController(EmpAnalysisDbContext context, ILogger<MonitoringController> logger, IHubContext<MonitoringHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Submit a complete monitoring session
    /// </summary>
    [HttpPost("session")]
    public async Task<IActionResult> SubmitSession([FromBody] MonitoringSessionDto sessionDto)
    {
        try
        {
            _logger.LogInformation("Received monitoring session for employee: {EmployeeId}", sessionDto.EmployeeId);

            // Find or create employee
            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == sessionDto.EmployeeId || e.Email == sessionDto.EmployeeId);

            if (employee == null)
            {
                // Create new employee record if not exists
                employee = new Employee
                {
                    UserName = sessionDto.EmployeeId,
                    Email = $"{sessionDto.EmployeeId}@company.com",
                    FirstName = sessionDto.EmployeeId,
                    LastName = "User",
                    Department = "Unknown",
                    JobTitle = "Employee",
                    IsActive = true,
                    StartDate = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                _context.Users.Add(employee);
                await _context.SaveChangesAsync();
            }

            // Update last activity
            employee.LastLoginAt = sessionDto.EndTime ?? DateTime.UtcNow;
            
            // Process applications
            foreach (var app in sessionDto.Applications)
            {
                var appUsage = new ApplicationUsage
                {
                    EmployeeId = employee.Id,
                    ApplicationName = app.ApplicationName,
                    ExecutablePath = app.ExecutablePath,
                    WindowTitle = app.WindowTitle,
                    StartTime = app.StartTime,
                    EndTime = app.EndTime,
                    Duration = app.Duration,
                    IsProductiveApplication = app.IsProductiveApp,
                    Category = app.Category
                };
                _context.ApplicationUsages.Add(appUsage);
            }

            // Process website visits
            foreach (var website in sessionDto.WebsiteVisits)
            {
                var websiteVisit = new WebsiteVisit
                {
                    EmployeeId = employee.Id,
                    Url = website.Url,
                    Title = website.Title,
                    Domain = website.Domain,
                    VisitStart = website.StartTime,
                    VisitEnd = website.EndTime,
                    Duration = website.Duration,
                    IsProductiveTime = website.IsProductiveSite,
                    Category = website.Category
                };
                _context.WebsiteVisits.Add(websiteVisit);
            }

            // Process screenshots
            foreach (var screenshot in sessionDto.Screenshots)
            {
                var screenshotRecord = new Screenshot
                {
                    EmployeeId = employee.Id,
                    CapturedAt = screenshot.Timestamp,
                    FilePath = screenshot.FilePath,
                    FileName = $"screenshot_{screenshot.Timestamp:yyyyMMdd_HHmmss}.jpg",
                    FileSizeBytes = screenshot.FileSize,
                    ActiveApplication = screenshot.ActiveApplication,
                    WindowTitle = screenshot.ActiveWindow
                };
                _context.Screenshots.Add(screenshotRecord);
            }

            // Process system events
            foreach (var sysEvent in sessionDto.SystemEvents)
            {
                var activityLog = new ActivityLog
                {
                    EmployeeId = employee.Id,
                    ActivityType = sysEvent.EventType,
                    Description = sysEvent.Description,
                    Timestamp = sysEvent.Timestamp
                };
                _context.ActivityLogs.Add(activityLog);
            }

            await _context.SaveChangesAsync();

            // Send real-time update to dashboard
            await _hubContext.Clients.Group("Dashboard").SendAsync("DashboardUpdate", new
            {
                type = "session",
                employeeId = employee.Id,
                employeeName = $"{employee.FirstName} {employee.LastName}",
                timestamp = DateTime.UtcNow,
                data = new
                {
                    applications = sessionDto.Applications.Count,
                    websites = sessionDto.WebsiteVisits.Count,
                    screenshots = sessionDto.Screenshots.Count,
                    systemEvents = sessionDto.SystemEvents.Count
                }
            });

            _logger.LogInformation("Successfully processed monitoring session for employee: {EmployeeId}", sessionDto.EmployeeId);

            return Ok(new { 
                message = "Session data received successfully",
                employeeId = employee.Id,
                processed = new
                {
                    applications = sessionDto.Applications.Count,
                    websites = sessionDto.WebsiteVisits.Count,
                    screenshots = sessionDto.Screenshots.Count,
                    systemEvents = sessionDto.SystemEvents.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing monitoring session for employee: {EmployeeId}", sessionDto.EmployeeId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Submit screenshot data
    /// </summary>
    [HttpPost("screenshot")]
    public async Task<IActionResult> SubmitScreenshot([FromBody] ScreenshotSubmissionDto screenshotDto)
    {
        try
        {
            _logger.LogInformation("Received screenshot for employee: {EmployeeId}", screenshotDto.EmployeeId);

            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == screenshotDto.EmployeeId || e.Email == screenshotDto.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new { error = "Employee not found", employeeId = screenshotDto.EmployeeId });
            }

            var screenshot = new Screenshot
            {
                EmployeeId = employee.Id,
                CapturedAt = screenshotDto.Timestamp,
                FileName = $"screenshot_{screenshotDto.Timestamp:yyyyMMdd_HHmmss}.jpg",
                FilePath = $"/screenshots/{employee.Id}/{screenshotDto.Timestamp:yyyy/MM/dd}/",
                FileSizeBytes = screenshotDto.FileSize,
                ActiveApplication = screenshotDto.ActiveApplication,
                WindowTitle = screenshotDto.ActiveWindow
            };

            _context.Screenshots.Add(screenshot);
            await _context.SaveChangesAsync();

            // Send real-time screenshot update to dashboard
            await _hubContext.Clients.Group("Dashboard").SendAsync("ScreenshotUpdate", new
            {
                type = "screenshot",
                employeeId = employee.Id,
                employeeName = $"{employee.FirstName} {employee.LastName}",
                timestamp = screenshot.CapturedAt,
                activeApplication = screenshot.ActiveApplication,
                windowTitle = screenshot.WindowTitle
            });

            _logger.LogInformation("Successfully processed screenshot for employee: {EmployeeId}, Size: {FileSize}", 
                screenshotDto.EmployeeId, screenshotDto.FileSize);

            return Ok(new { 
                message = "Screenshot received successfully",
                screenshotId = screenshot.Id,
                timestamp = screenshot.CapturedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing screenshot for employee: {EmployeeId}", screenshotDto.EmployeeId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Submit activity data (applications and websites)
    /// </summary>
    [HttpPost("activity")]
    public async Task<IActionResult> SubmitActivity([FromBody] ActivitySubmissionDto activityDto)
    {
        try
        {
            _logger.LogInformation("Received activity data for employee: {EmployeeId}", activityDto.EmployeeId);

            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == activityDto.EmployeeId || e.Email == activityDto.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new { error = "Employee not found", employeeId = activityDto.EmployeeId });
            }

            // Process applications
            foreach (var app in activityDto.Applications)
            {
                var appUsage = new ApplicationUsage
                {
                    EmployeeId = employee.Id,
                    ApplicationName = app.ApplicationName,
                    ExecutablePath = app.ExecutablePath,
                    WindowTitle = app.WindowTitle,
                    StartTime = app.StartTime,
                    EndTime = app.EndTime,
                    Duration = app.Duration,
                    IsProductiveApplication = app.IsProductiveApp,
                    Category = app.Category
                };
                _context.ApplicationUsages.Add(appUsage);
            }

            // Process website visits
            foreach (var website in activityDto.Websites)
            {
                var websiteVisit = new WebsiteVisit
                {
                    EmployeeId = employee.Id,
                    Url = website.Url,
                    Title = website.Title,
                    Domain = website.Domain,
                    VisitStart = website.StartTime,
                    VisitEnd = website.EndTime,
                    Duration = website.Duration,
                    IsProductiveTime = website.IsProductiveSite,
                    Category = website.Category
                };
                _context.WebsiteVisits.Add(websiteVisit);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed activity data for employee: {EmployeeId}, Apps: {AppCount}, Websites: {WebsiteCount}", 
                activityDto.EmployeeId, activityDto.Applications.Count, activityDto.Websites.Count);

            return Ok(new { 
                message = "Activity data received successfully",
                processed = new
                {
                    applications = activityDto.Applications.Count,
                    websites = activityDto.Websites.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity data for employee: {EmployeeId}", activityDto.EmployeeId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Submit system events
    /// </summary>
    [HttpPost("system-events")]
    public async Task<IActionResult> SubmitSystemEvents([FromBody] SystemEventsSubmissionDto eventsDto)
    {
        try
        {
            _logger.LogInformation("Received system events for employee: {EmployeeId}", eventsDto.EmployeeId);

            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == eventsDto.EmployeeId || e.Email == eventsDto.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new { error = "Employee not found", employeeId = eventsDto.EmployeeId });
            }

            // Process system events
            foreach (var sysEvent in eventsDto.Events)
            {
                var activityLog = new ActivityLog
                {
                    EmployeeId = employee.Id,
                    ActivityType = sysEvent.EventType,
                    Description = sysEvent.Description,
                    Timestamp = sysEvent.Timestamp
                };
                _context.ActivityLogs.Add(activityLog);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed system events for employee: {EmployeeId}, Events: {EventCount}", 
                eventsDto.EmployeeId, eventsDto.Events.Count);

            return Ok(new { 
                message = "System events received successfully",
                processed = new
                {
                    events = eventsDto.Events.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing system events for employee: {EmployeeId}", eventsDto.EmployeeId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get live dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData()
    {
        try
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var last5Minutes = DateTime.UtcNow.AddMinutes(-5);
            var lastHour = DateTime.UtcNow.AddHours(-1);

            // Get real dashboard metrics
            var totalEmployees = await _context.Users.Where(e => e.IsActive).CountAsync();
            var productiveHours = await CalculateProductiveHoursAsync(thisWeek, today.AddDays(1));
            var totalScreenshots = await _context.Screenshots.Where(s => s.CapturedAt >= today).CountAsync();
            var securityAlerts = await _context.ActivityLogs
                .Where(a => a.Timestamp >= today && (a.ActivityType == ActivityType.UsbDevice || a.ActivityType == ActivityType.SystemEvent))
                .CountAsync();
            var productivityScore = await CalculateProductivityScoreAsync();
            var networkEvents = await _context.ActivityLogs
                .Where(a => a.Timestamp >= today && a.ActivityType == ActivityType.SystemEvent)
                .CountAsync();

            // Get employee status counts
            var employees = await _context.Users.Where(e => e.IsActive).Select(e => e.LastLoginAt).ToListAsync();
            var online = employees.Count(e => e >= last5Minutes);
            var idle = employees.Count(e => e >= lastHour && e < last5Minutes);
            var offline = employees.Count(e => e < lastHour);

            // Get recent activities
            var recentActivities = await _context.ActivityLogs
                .Include(a => a.Employee)
                .Where(a => a.Timestamp >= today)
                .OrderByDescending(a => a.Timestamp)
                .Take(15)
                .Select(a => new
                {
                    id = a.Id,
                    employeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    description = a.Description,
                    timestamp = a.Timestamp,
                    activityType = a.ActivityType
                })
                .ToListAsync();

            // Get top applications
            var todayApps = await _context.ApplicationUsages
                .Where(a => a.StartTime >= today)
                .ToListAsync();

            var topApplications = todayApps
                .GroupBy(a => a.ApplicationName)
                .Select(g => new
                {
                    applicationName = g.Key,
                    totalHours = g.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value.TotalHours),
                    usageCount = g.Count()
                })
                .OrderByDescending(x => x.totalHours)
                .Take(8)
                .ToList();

            // Get recent alerts
            var recentAlerts = await _context.ActivityLogs
                .Include(a => a.Employee)
                .Where(a => a.Timestamp >= today.AddDays(-7))
                .Where(a => a.ActivityType == ActivityType.UsbDevice || a.ActivityType == ActivityType.SystemEvent)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new
                {
                    id = a.Id,
                    description = a.Description,
                    timestamp = a.Timestamp,
                    employeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    activityType = a.ActivityType
                })
                .ToListAsync();

            var dashboardData = new
            {
                totalEmployees,
                totalProductiveHours = productiveHours,
                totalScreenshots,
                securityAlerts,
                productivityScore,
                networkEvents,
                onlineEmployees = online,
                idleEmployees = idle,
                offlineEmployees = offline,
                recentActivities,
                topApplications,
                recentAlerts,
                lastUpdated = DateTime.UtcNow
            };

            // Send real-time update to all dashboard clients
            await _hubContext.Clients.Group("Dashboard").SendAsync("DashboardUpdate", dashboardData);

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    private async Task<double> CalculateProductiveHoursAsync(DateTime startDate, DateTime endDate)
    {
        var productiveApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= startDate && a.StartTime < endDate && a.IsProductiveApplication)
            .ToListAsync();

        return productiveApps.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value.TotalHours);
    }

    private async Task<double> CalculateProductivityScoreAsync()
    {
        var today = DateTime.Today;
        var allApps = await _context.ApplicationUsages
            .Where(a => a.StartTime >= today)
            .ToListAsync();

        if (!allApps.Any()) return 0.0;

        var totalHours = allApps.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value.TotalHours);
        var productiveHours = allApps.Where(a => a.IsProductiveApplication && a.Duration.HasValue)
            .Sum(a => a.Duration!.Value.TotalHours);

        return totalHours > 0 ? (productiveHours / totalHours) * 100 : 0.0;
    }
}

// DTOs for agent communication
public class MonitoringSessionDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<ApplicationUsageDto> Applications { get; set; } = new();
    public List<WebsiteVisitDto> WebsiteVisits { get; set; } = new();
    public List<ScreenshotCaptureDto> Screenshots { get; set; } = new();
    public List<SystemEventDto> SystemEvents { get; set; } = new();
}

public class ApplicationUsageDto
{
    public string ApplicationName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsProductiveApp { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class WebsiteVisitDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsProductiveSite { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ScreenshotCaptureDto
{
    public DateTime Timestamp { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Base64Data { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public string ActiveApplication { get; set; } = string.Empty;
    public string ActiveWindow { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
}

public class SystemEventDto
{
    public DateTime Timestamp { get; set; }
    public ActivityType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class ScreenshotSubmissionDto
{
    public DateTime Timestamp { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string ActiveApplication { get; set; } = string.Empty;
    public string ActiveWindow { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public string ImageData { get; set; } = string.Empty;
}

public class ActivitySubmissionDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<ApplicationUsageDto> Applications { get; set; } = new();
    public List<WebsiteVisitDto> Websites { get; set; } = new();
}

public class SystemEventsSubmissionDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<SystemEventDto> Events { get; set; } = new();
}

public enum SystemEventType
{
    Login,
    Logout,
    Lock,
    Unlock,
    Idle,
    Active,
    ApplicationStart,
    ApplicationEnd,
    FileAccess,
    NetworkActivity,
    USBInsert,
    USBRemove,
    PrintJob
} 