using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class ActivityLog
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public ActivityType ActivityType { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(200)]
    public string? ApplicationName { get; set; }

    [StringLength(500)]
    public string? WindowTitle { get; set; }

    [StringLength(1000)]
    public string? Url { get; set; }

    public TimeSpan? Duration { get; set; }

    public bool IsIdleTime { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public decimal? ProductivityScore { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}

public enum ActivityType
{
    Login = 1,
    Logout = 2,
    ApplicationStart = 3,
    ApplicationEnd = 4,
    WebsiteVisit = 5,
    FileAccess = 6,
    IdleStart = 7,
    IdleEnd = 8,
    ScreenCapture = 9,
    KeystrokeActivity = 10,
    PrintJob = 11,
    EmailActivity = 12,
    UsbDevice = 13,
    SystemEvent = 14
} 