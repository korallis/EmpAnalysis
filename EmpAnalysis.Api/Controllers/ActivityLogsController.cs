using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityLogsController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<ActivityLogsController> _logger;

    public ActivityLogsController(EmpAnalysisDbContext context, ILogger<ActivityLogsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get activity logs with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetActivityLogs(
        [FromQuery] string? employeeId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ActivityType? activityType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.ActivityLogs.Include(a => a.Employee).AsQueryable();

        if (!string.IsNullOrEmpty(employeeId))
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        if (activityType.HasValue)
        {
            query = query.Where(a => a.ActivityType == activityType.Value);
        }

        var total = await query.CountAsync();
        var activities = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                a.ActivityType,
                a.Description,
                a.Timestamp,
                a.ApplicationName,
                a.WindowTitle,
                a.Url,
                a.Duration,
                a.IsIdleTime,
                a.Category,
                a.ProductivityScore
            })
            .ToListAsync();

        return Ok(new
        {
            Data = activities,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>
    /// Create a new activity log entry
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ActivityLog>> CreateActivityLog([FromBody] CreateActivityLogDto dto)
    {
        var employee = await _context.Users.FindAsync(dto.EmployeeId);
        if (employee == null)
        {
            return BadRequest("Employee not found");
        }

        var activityLog = new ActivityLog
        {
            EmployeeId = dto.EmployeeId,
            ActivityType = dto.ActivityType,
            Description = dto.Description,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            ApplicationName = dto.ApplicationName,
            WindowTitle = dto.WindowTitle,
            Url = dto.Url,
            Duration = dto.Duration,
            IsIdleTime = dto.IsIdleTime,
            Category = dto.Category,
            ProductivityScore = dto.ProductivityScore
        };

        _context.ActivityLogs.Add(activityLog);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActivityLog), new { id = activityLog.Id }, activityLog);
    }

    /// <summary>
    /// Get a specific activity log by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetActivityLog(int id)
    {
        var activity = await _context.ActivityLogs
            .Include(a => a.Employee)
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                a.ActivityType,
                a.Description,
                a.Timestamp,
                a.ApplicationName,
                a.WindowTitle,
                a.Url,
                a.Duration,
                a.IsIdleTime,
                a.Category,
                a.ProductivityScore
            })
            .FirstOrDefaultAsync();

        if (activity == null)
        {
            return NotFound();
        }

        return Ok(activity);
    }

    /// <summary>
    /// Get activity statistics for dashboard
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetActivityStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.Date.AddDays(-7);
        endDate ??= DateTime.UtcNow.Date.AddDays(1);

        var activities = await _context.ActivityLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp < endDate)
            .ToListAsync();

        var totalActivities = activities.Count;
        var idleTime = activities.Where(a => a.IsIdleTime).Sum(a => a.Duration?.TotalMinutes ?? 0);
        var activeTime = activities.Where(a => !a.IsIdleTime).Sum(a => a.Duration?.TotalMinutes ?? 0);

        var activityTypeBreakdown = activities
            .GroupBy(a => a.ActivityType)
            .Select(g => new { ActivityType = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var hourlyActivity = activities
            .GroupBy(a => a.Timestamp.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderBy(x => x.Hour)
            .ToList();

        var topApplications = activities
            .Where(a => !string.IsNullOrEmpty(a.ApplicationName))
            .GroupBy(a => a.ApplicationName)
            .Select(g => new { Application = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        return Ok(new
        {
            Period = new { StartDate = startDate, EndDate = endDate },
            TotalActivities = totalActivities,
            IdleTimeMinutes = idleTime,
            ActiveTimeMinutes = activeTime,
            ActivityTypeBreakdown = activityTypeBreakdown,
            HourlyActivity = hourlyActivity,
            TopApplications = topApplications
        });
    }

    /// <summary>
    /// Bulk create activity logs (for agent uploads)
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult> CreateActivityLogsBulk([FromBody] List<CreateActivityLogDto> activities)
    {
        if (activities == null || !activities.Any())
        {
            return BadRequest("No activities provided");
        }

        var employeeIds = activities.Select(a => a.EmployeeId).Distinct().ToList();
        var existingEmployees = await _context.Users
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        var validActivities = activities
            .Where(a => existingEmployees.Contains(a.EmployeeId))
            .Select(dto => new ActivityLog
            {
                EmployeeId = dto.EmployeeId,
                ActivityType = dto.ActivityType,
                Description = dto.Description,
                Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                ApplicationName = dto.ApplicationName,
                WindowTitle = dto.WindowTitle,
                Url = dto.Url,
                Duration = dto.Duration,
                IsIdleTime = dto.IsIdleTime,
                Category = dto.Category,
                ProductivityScore = dto.ProductivityScore
            })
            .ToList();

        if (validActivities.Any())
        {
            _context.ActivityLogs.AddRange(validActivities);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            Processed = validActivities.Count,
            Skipped = activities.Count - validActivities.Count,
            Message = $"Successfully processed {validActivities.Count} activity logs"
        });
    }
}

public class CreateActivityLogDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string? ApplicationName { get; set; }
    public string? WindowTitle { get; set; }
    public string? Url { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsIdleTime { get; set; }
    public string? Category { get; set; }
    public decimal? ProductivityScore { get; set; }
} 