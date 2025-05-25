using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class FileAccessLog
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(255)]
    public string? FileName { get; set; }

    [StringLength(50)]
    public string? FileExtension { get; set; }

    public FileAccessType AccessType { get; set; }

    public DateTime AccessTime { get; set; } = DateTime.UtcNow;

    public long? FileSizeBytes { get; set; }

    [StringLength(200)]
    public string? ApplicationUsed { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public bool IsSensitiveFile { get; set; }

    public bool IsCloudFile { get; set; }

    [StringLength(100)]
    public string? CloudProvider { get; set; }

    public bool IsEncrypted { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}

public enum FileAccessType
{
    Read = 1,
    Write = 2,
    Create = 3,
    Delete = 4,
    Rename = 5,
    Copy = 6,
    Move = 7,
    Print = 8,
    Email = 9,
    Upload = 10,
    Download = 11
} 