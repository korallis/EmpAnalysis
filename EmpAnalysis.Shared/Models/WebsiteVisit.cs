using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class WebsiteVisit
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Url { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Title { get; set; }

    [StringLength(200)]
    public string? Domain { get; set; }

    public DateTime VisitStart { get; set; } = DateTime.UtcNow;

    public DateTime? VisitEnd { get; set; }

    public TimeSpan? Duration { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public bool IsProductiveTime { get; set; }

    public bool IsBlocked { get; set; }

    [StringLength(200)]
    public string? Browser { get; set; }

    [StringLength(100)]
    public string? SearchQuery { get; set; }

    public int PageViews { get; set; } = 1;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
} 