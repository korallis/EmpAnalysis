using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScreenshotsController : ControllerBase
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<ScreenshotsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ScreenshotsController(
        EmpAnalysisDbContext context, 
        ILogger<ScreenshotsController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get screenshots with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetScreenshots(
        [FromQuery] string? employeeId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Screenshots
            .Include(s => s.Employee)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(employeeId))
        {
            query = query.Where(s => s.EmployeeId == employeeId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CapturedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CapturedAt <= endDate.Value);
        }

        var total = await query.CountAsync();
        var screenshots = await query
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.EmployeeId,
                EmployeeName = s.Employee.FirstName + " " + s.Employee.LastName,
                s.FileName,
                s.CapturedAt,
                s.FileSizeBytes,
                s.Width,
                s.Height,
                s.ActiveApplication,
                s.WindowTitle,
                s.ActiveUrl,
                s.IsBlurred,
                s.CompressionFormat,
                s.CompressionQuality,
                ThumbnailUrl = $"/api/screenshots/{s.Id}/thumbnail",
                DownloadUrl = $"/api/screenshots/{s.Id}/download"
            })
            .ToListAsync();

        return Ok(new
        {
            Data = screenshots,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>
    /// Get a specific screenshot by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetScreenshot(int id)
    {
        var screenshot = await _context.Screenshots
            .Include(s => s.Employee)
            .Where(s => s.Id == id && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.EmployeeId,
                EmployeeName = s.Employee.FirstName + " " + s.Employee.LastName,
                s.FileName,
                s.FilePath,
                s.CapturedAt,
                s.FileSizeBytes,
                s.Width,
                s.Height,
                s.ActiveApplication,
                s.WindowTitle,
                s.ActiveUrl,
                s.IsBlurred,
                s.CompressionFormat,
                s.CompressionQuality,
                DownloadUrl = $"/api/screenshots/{s.Id}/download"
            })
            .FirstOrDefaultAsync();

        if (screenshot == null)
        {
            return NotFound();
        }

        return Ok(screenshot);
    }

    /// <summary>
    /// Upload a new screenshot
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<object>> UploadScreenshot([FromForm] UploadScreenshotDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest("No file provided");
        }

        var employee = await _context.Users.FindAsync(dto.EmployeeId);
        if (employee == null)
        {
            return BadRequest("Employee not found");
        }

        // Create screenshots directory if it doesn't exist
        var screenshotsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "screenshots");
        Directory.CreateDirectory(screenshotsPath);

        // Generate unique filename
        var fileExtension = Path.GetExtension(dto.File.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(screenshotsPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        // Create screenshot record
        var screenshot = new Screenshot
        {
            EmployeeId = dto.EmployeeId,
            FileName = fileName,
            FilePath = $"/screenshots/{fileName}",
            CapturedAt = dto.CapturedAt ?? DateTime.UtcNow,
            FileSizeBytes = dto.File.Length,
            Width = dto.Width,
            Height = dto.Height,
            ActiveApplication = dto.ActiveApplication,
            WindowTitle = dto.WindowTitle,
            ActiveUrl = dto.ActiveUrl,
            IsBlurred = dto.IsBlurred,
            CompressionFormat = dto.CompressionFormat ?? "JPEG",
            CompressionQuality = dto.CompressionQuality ?? 85
        };

        _context.Screenshots.Add(screenshot);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetScreenshot), new { id = screenshot.Id }, new
        {
            screenshot.Id,
            screenshot.FileName,
            screenshot.CapturedAt,
            DownloadUrl = $"/api/screenshots/{screenshot.Id}/download"
        });
    }

    /// <summary>
    /// Download screenshot file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult> DownloadScreenshot(int id)
    {
        var screenshot = await _context.Screenshots
            .Where(s => s.Id == id && !s.IsDeleted)
            .FirstOrDefaultAsync();

        if (screenshot == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", screenshot.FilePath.TrimStart('/'));
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found on disk");
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var contentType = screenshot.CompressionFormat.ToLower() switch
        {
            "png" => "image/png",
            "jpeg" or "jpg" => "image/jpeg",
            "gif" => "image/gif",
            _ => "application/octet-stream"
        };

        return File(fileBytes, contentType, screenshot.FileName);
    }

    /// <summary>
    /// Get screenshot thumbnail (smaller version)
    /// </summary>
    [HttpGet("{id}/thumbnail")]
    public async Task<ActionResult> GetThumbnail(int id, [FromQuery] int width = 200, [FromQuery] int height = 150)
    {
        var screenshot = await _context.Screenshots
            .Where(s => s.Id == id && !s.IsDeleted)
            .FirstOrDefaultAsync();

        if (screenshot == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", screenshot.FilePath.TrimStart('/'));
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found on disk");
        }

        // For now, return the original file. In production, you'd want to generate actual thumbnails
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var contentType = screenshot.CompressionFormat.ToLower() switch
        {
            "png" => "image/png",
            "jpeg" or "jpg" => "image/jpeg",
            "gif" => "image/gif",
            _ => "application/octet-stream"
        };

        return File(fileBytes, contentType);
    }

    /// <summary>
    /// Delete a screenshot (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteScreenshot(int id)
    {
        var screenshot = await _context.Screenshots.FindAsync(id);
        if (screenshot == null)
        {
            return NotFound();
        }

        screenshot.IsDeleted = true;
        screenshot.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get screenshot statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetScreenshotStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.Date.AddDays(-7);
        endDate ??= DateTime.UtcNow.Date.AddDays(1);

        var screenshots = await _context.Screenshots
            .Where(s => !s.IsDeleted && s.CapturedAt >= startDate && s.CapturedAt < endDate)
            .ToListAsync();

        var totalScreenshots = screenshots.Count;
        var totalSizeBytes = screenshots.Sum(s => s.FileSizeBytes);
        var avgSizeBytes = screenshots.Any() ? screenshots.Average(s => s.FileSizeBytes) : 0;

        var screenshotsByEmployee = screenshots
            .GroupBy(s => s.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var screenshotsByHour = screenshots
            .GroupBy(s => s.CapturedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderBy(x => x.Hour)
            .ToList();

        var topApplications = screenshots
            .Where(s => !string.IsNullOrEmpty(s.ActiveApplication))
            .GroupBy(s => s.ActiveApplication)
            .Select(g => new { Application = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        return Ok(new
        {
            Period = new { StartDate = startDate, EndDate = endDate },
            TotalScreenshots = totalScreenshots,
            TotalSizeBytes = totalSizeBytes,
            TotalSizeMB = Math.Round(totalSizeBytes / (1024.0 * 1024.0), 2),
            AverageSizeBytes = Math.Round(avgSizeBytes),
            ScreenshotsByEmployee = screenshotsByEmployee,
            ScreenshotsByHour = screenshotsByHour,
            TopApplications = topApplications
        });
    }
}

public class UploadScreenshotDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public DateTime? CapturedAt { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? ActiveApplication { get; set; }
    public string? WindowTitle { get; set; }
    public string? ActiveUrl { get; set; }
    public bool IsBlurred { get; set; }
    public string? CompressionFormat { get; set; }
    public decimal? CompressionQuality { get; set; }
} 