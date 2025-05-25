using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class ApplicationUsage
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ApplicationName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ExecutablePath { get; set; }

    [StringLength(500)]
    public string? WindowTitle { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public TimeSpan? Duration { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public bool IsProductiveApplication { get; set; }

    public bool IsBlocked { get; set; }

    public decimal ProductivityScore { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    public int FocusCount { get; set; } = 1;

    public long MemoryUsage { get; set; }

    public decimal CpuUsage { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
} 