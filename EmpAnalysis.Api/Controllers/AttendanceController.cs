using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;

    public AttendanceController(EmpAnalysisDbContext context)
    {
        _context = context;
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetAttendanceLogs(string employeeId)
    {
        var logs = await _context.AttendanceLogs
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpPost]
    public async Task<IActionResult> LogAttendance([FromBody] AttendanceLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        _context.AttendanceLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok(log);
    }
}
