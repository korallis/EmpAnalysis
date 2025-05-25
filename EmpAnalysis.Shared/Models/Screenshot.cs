using System.ComponentModel.DataAnnotations;

namespace EmpAnalysis.Shared.Models;

public class Screenshot
{
    public int Id { get; set; }

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public long FileSizeBytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    [StringLength(200)]
    public string? ActiveApplication { get; set; }

    [StringLength(500)]
    public string? WindowTitle { get; set; }

    [StringLength(1000)]
    public string? ActiveUrl { get; set; }

    public bool IsBlurred { get; set; }

    [StringLength(50)]
    public string CompressionFormat { get; set; } = "JPEG";

    public decimal CompressionQuality { get; set; } = 85;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
} 