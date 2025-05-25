using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;
using System.Security.Claims;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<AgentController> _logger;

    public AgentController(EmpAnalysisDbContext context, ILogger<AgentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new monitoring agent
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous] // Allow anonymous for agent registration
    public async Task<IActionResult> RegisterAgent([FromBody] AgentRegistrationDto registrationDto)
    {
        try
        {
            _logger.LogInformation("Agent registration request for employee: {EmployeeId}, Machine: {MachineName}", 
                registrationDto.EmployeeId, registrationDto.MachineName);

            // Find or create employee
            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == registrationDto.EmployeeId || e.Email == registrationDto.EmployeeId);

            if (employee == null)
            {
                // Create new employee record
                employee = new Employee
                {
                    UserName = registrationDto.EmployeeId,
                    Email = $"{registrationDto.EmployeeId}@company.com",
                    FirstName = registrationDto.EmployeeId,
                    LastName = "User",
                    Department = "Unknown",
                    JobTitle = "Employee",
                    IsActive = true,
                    StartDate = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                _context.Users.Add(employee);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new employee record for: {EmployeeId}", registrationDto.EmployeeId);
            }

            // Generate unique agent ID
            var agentId = $"AGT-{employee.Id[..6]}-{Guid.NewGuid():N}"[..16];

            // Log agent registration
            var activityLog = new ActivityLog
            {
                EmployeeId = employee.Id,
                ActivityType = ActivityType.SystemEvent,
                Description = "Monitoring agent registered",
                Timestamp = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(activityLog);

            // Update employee last activity
            employee.LastLoginAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            var response = new AgentRegistrationResponse
            {
                AgentId = agentId,
                Status = "Success",
                RegistrationTime = DateTime.UtcNow,
                Configuration = new Dictionary<string, object>
                {
                    { "screenshotInterval", 300 },
                    { "activityInterval", 30 },
                    { "syncInterval", 300 },
                    { "enableScreenshots", true },
                    { "enableActivityTracking", true },
                    { "enableWebsiteTracking", true },
                    { "workingHours", new { start = "09:00", end = "17:00" } }
                }
            };

            _logger.LogInformation("Agent registered successfully. Agent ID: {AgentId}, Employee: {EmployeeId}", 
                agentId, registrationDto.EmployeeId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent for employee: {EmployeeId}", registrationDto.EmployeeId);
            return StatusCode(500, new { error = "Registration failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get agent configuration
    /// </summary>
    [HttpGet("config/{agentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAgentConfig(string agentId)
    {
        try
        {
            _logger.LogInformation("Agent configuration request for Agent ID: {AgentId}", agentId);

            var config = new
            {
                screenshotInterval = 300,
                activityInterval = 30,
                syncInterval = 300,
                enableScreenshots = true,
                enableActivityTracking = true,
                enableWebsiteTracking = true,
                enableKeylogger = false,
                workingHours = new { start = "09:00", end = "17:00" },
                productivityCategories = new[]
                {
                    new { name = "Development", productive = true },
                    new { name = "Office", productive = true },
                    new { name = "Communication", productive = true },
                    new { name = "Web Browser", productive = true },
                    new { name = "Media", productive = false },
                    new { name = "Gaming", productive = false }
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent configuration for Agent ID: {AgentId}", agentId);
            return StatusCode(500, new { error = "Configuration retrieval failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Agent heartbeat/health check
    /// </summary>
    [HttpPost("heartbeat")]
    [AllowAnonymous]
    public async Task<IActionResult> AgentHeartbeat([FromBody] AgentHeartbeatDto heartbeatDto)
    {
        try
        {
            _logger.LogDebug("Agent heartbeat from Agent ID: {AgentId}", heartbeatDto.AgentId);

            // Log heartbeat activity
            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == heartbeatDto.EmployeeId || e.Email == heartbeatDto.EmployeeId);

            if (employee != null)
            {
                employee.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { 
                status = "OK", 
                timestamp = DateTime.UtcNow,
                nextHeartbeat = DateTime.UtcNow.AddMinutes(5)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent heartbeat for Agent ID: {AgentId}", heartbeatDto.AgentId);
            return StatusCode(500, new { error = "Heartbeat failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Agent status update
    /// </summary>
    [HttpPost("status")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateAgentStatus([FromBody] AgentStatusDto statusDto)
    {
        try
        {
            _logger.LogInformation("Agent status update from Agent ID: {AgentId}, Status: {Status}", 
                statusDto.AgentId, statusDto.Status);

            var employee = await _context.Users
                .FirstOrDefaultAsync(e => e.UserName == statusDto.EmployeeId || e.Email == statusDto.EmployeeId);

            if (employee != null)
            {
                var activityLog = new ActivityLog
                {
                    EmployeeId = employee.Id,
                    ActivityType = ActivityType.SystemEvent,
                    Description = $"Agent status: {statusDto.Status}",
                    Timestamp = DateTime.UtcNow
                };
                _context.ActivityLogs.Add(activityLog);
                
                employee.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { status = "Updated", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent status for Agent ID: {AgentId}", statusDto.AgentId);
            return StatusCode(500, new { error = "Status update failed", message = ex.Message });
        }
    }
}

// DTOs for agent registration and management
public class AgentRegistrationDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; }
}

public class AgentRegistrationResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class AgentHeartbeatDto
{
    public string AgentId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Online";
}

public class AgentStatusDto
{
    public string AgentId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public DateTime Timestamp { get; set; }
} 