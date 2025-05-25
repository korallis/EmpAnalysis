using System;
using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class AttendanceLog
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public AttendanceEventType EventType { get; set; }

    public string? Notes { get; set; }
}

public enum AttendanceEventType
{
    ClockIn,
    ClockOut,
    BreakStart,
    BreakEnd
}
