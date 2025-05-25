using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;
    private readonly UserManager<Employee> _userManager;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        EmpAnalysisDbContext context,
        UserManager<Employee> userManager,
        ILogger<EmployeesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees with pagination and search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? department = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e => e.FirstName.Contains(search) || 
                                   e.LastName.Contains(search) || 
                                   e.Email.Contains(search));
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(e => e.Department == department);
        }

        var total = await query.CountAsync();
        var employees = await query
            .OrderBy(e => e.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Department,
                e.JobTitle,
                e.StartDate,
                e.IsActive,
                e.LastLoginAt,
                FullName = e.FirstName + " " + e.LastName
            })
            .ToListAsync();

        return Ok(new
        {
            Data = employees,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetEmployee(string id)
    {
        var employee = await _context.Users
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Department,
                e.JobTitle,
                e.StartDate,
                e.IsActive,
                e.LastLoginAt,
                e.CreatedAt,
                FullName = e.FirstName + " " + e.LastName
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    /// <summary>
    /// Get employee activity summary
    /// </summary>
    [HttpGet("{id}/activity-summary")]
    public async Task<ActionResult<object>> GetEmployeeActivitySummary(
        string id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var employee = await _context.Users.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        startDate ??= DateTime.UtcNow.Date.AddDays(-7);
        endDate ??= DateTime.UtcNow.Date.AddDays(1);

        var activities = await _context.ActivityLogs
            .Where(a => a.EmployeeId == id && a.Timestamp >= startDate && a.Timestamp < endDate)
            .ToListAsync();

        var screenshots = await _context.Screenshots
            .Where(s => s.EmployeeId == id && s.CapturedAt >= startDate && s.CapturedAt < endDate)
            .CountAsync();

        var websiteVisits = await _context.WebsiteVisits
            .Where(w => w.EmployeeId == id && w.VisitStart >= startDate && w.VisitStart < endDate)
            .ToListAsync();

        var applicationUsage = await _context.ApplicationUsages
            .Where(a => a.EmployeeId == id && a.StartTime >= startDate && a.StartTime < endDate)
            .ToListAsync();

        return Ok(new
        {
            EmployeeId = id,
            Period = new { StartDate = startDate, EndDate = endDate },
            TotalActivities = activities.Count,
            TotalScreenshots = screenshots,
            TotalWebsiteVisits = websiteVisits.Count,
            TotalApplicationSessions = applicationUsage.Count,
            ProductiveTime = websiteVisits.Where(w => w.IsProductiveTime).Sum(w => w.Duration?.TotalMinutes ?? 0),
            ActivityBreakdown = activities.GroupBy(a => a.ActivityType)
                .Select(g => new { ActivityType = g.Key.ToString(), Count = g.Count() })
                .ToList(),
            TopApplications = applicationUsage.GroupBy(a => a.ApplicationName)
                .Select(g => new { Application = g.Key, Sessions = g.Count(), TotalTime = g.Sum(x => x.Duration?.TotalMinutes ?? 0) })
                .OrderByDescending(x => x.TotalTime)
                .Take(10)
                .ToList(),
            TopWebsites = websiteVisits.GroupBy(w => w.Domain)
                .Select(g => new { Domain = g.Key, Visits = g.Count(), TotalTime = g.Sum(x => x.Duration?.TotalMinutes ?? 0) })
                .OrderByDescending(x => x.TotalTime)
                .Take(10)
                .ToList()
        });
    }

    /// <summary>
    /// Get all departments
    /// </summary>
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<string>>> GetDepartments()
    {
        var departments = await _context.Users
            .Where(e => !string.IsNullOrEmpty(e.Department))
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        return Ok(departments);
    }

    /// <summary>
    /// Update employee information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _userManager.FindByIdAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Department = dto.Department;
        employee.JobTitle = dto.JobTitle;
        employee.IsActive = dto.IsActive;

        var result = await _userManager.UpdateAsync(employee);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Get employee statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetEmployeeStatistics()
    {
        var totalEmployees = await _context.Users.CountAsync();
        var activeEmployees = await _context.Users.CountAsync(e => e.IsActive);
        var employeesWithRecentActivity = await _context.Users
            .CountAsync(e => e.ActivityLogs.Any(a => a.Timestamp >= DateTime.UtcNow.AddDays(-1)));

        var departmentCounts = await _context.Users
            .Where(e => !string.IsNullOrEmpty(e.Department))
            .GroupBy(e => e.Department)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            InactiveEmployees = totalEmployees - activeEmployees,
            EmployeesWithRecentActivity = employeesWithRecentActivity,
            DepartmentBreakdown = departmentCounts
        });
    }
}

public class UpdateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; }
} 