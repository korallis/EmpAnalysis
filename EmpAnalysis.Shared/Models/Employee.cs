using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class Employee : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? ManagerId { get; set; }

    // Navigation properties
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public virtual ICollection<Screenshot> Screenshots { get; set; } = new List<Screenshot>();
    public virtual ICollection<WebsiteVisit> WebsiteVisits { get; set; } = new List<WebsiteVisit>();
    public virtual ICollection<ApplicationUsage> ApplicationUsages { get; set; } = new List<ApplicationUsage>();
    public virtual ICollection<FileAccessLog> FileAccesses { get; set; } = new List<FileAccessLog>();
    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
}