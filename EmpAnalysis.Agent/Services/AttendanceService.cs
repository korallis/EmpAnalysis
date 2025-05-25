using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EmpAnalysis.Shared.Models;
using Microsoft.Extensions.Logging;

namespace EmpAnalysis.Agent.Services;

public interface IAttendanceService
{
    Task<bool> LogAttendanceAsync(string employeeId, AttendanceEventType eventType, string? notes = null);
}

public class AttendanceService : IAttendanceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(HttpClient httpClient, ILogger<AttendanceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> LogAttendanceAsync(string employeeId, AttendanceEventType eventType, string? notes = null)
    {
        var log = new AttendanceLog
        {
            EmployeeId = employeeId,
            EventType = eventType,
            Notes = notes
        };
        var json = JsonSerializer.Serialize(log);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync("api/attendance", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log attendance event");
            return false;
        }
    }
}
