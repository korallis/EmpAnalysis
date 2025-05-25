using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using EmpAnalysis.Agent.Configuration;
using EmpAnalysis.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmpAnalysis.Agent.Services;

public interface IScreenshotService
{
    Task<ScreenshotCapture?> CaptureScreenshotAsync();
    Task<bool> SaveScreenshotAsync(ScreenshotCapture screenshot, string directoryPath);
}

public class ScreenshotService : IScreenshotService
{
    private readonly MonitoringSettings _settings;
    private readonly ILogger<ScreenshotService> _logger;
    private readonly IActiveWindowService _activeWindowService;

    public ScreenshotService(
        IOptions<AgentSettings> settings,
        ILogger<ScreenshotService> logger,
        IActiveWindowService activeWindowService)
    {
        _settings = settings.Value.MonitoringSettings;
        _logger = logger;
        _activeWindowService = activeWindowService;
    }

    public async Task<ScreenshotCapture?> CaptureScreenshotAsync()
    {
        try
        {
            _logger.LogDebug("Capturing screenshot...");

            var activeWindow = await _activeWindowService.GetActiveWindowInfoAsync();
            var screenshot = CaptureDesktop();

            if (screenshot == null)
            {
                _logger.LogWarning("Failed to capture desktop screenshot");
                return null;
            }

            string base64Data;
            int fileSize;

            using (var ms = new MemoryStream())
            {
                var codec = GetEncoderInfo("image/jpeg");
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)_settings.ScreenshotQuality);

                screenshot.Save(ms, codec, encoderParams);
                var imageBytes = ms.ToArray();
                base64Data = Convert.ToBase64String(imageBytes);
                fileSize = imageBytes.Length;
            }

            var result = new ScreenshotCapture
            {
                Timestamp = DateTime.UtcNow,
                Base64Data = base64Data,
                FileSize = fileSize,
                ActiveApplication = activeWindow?.ApplicationName ?? "Unknown",
                ActiveWindow = activeWindow?.WindowTitle ?? "Unknown",
                IsCompressed = true
            };

            _logger.LogDebug($"Screenshot captured successfully. Size: {fileSize} bytes, Active: {result.ActiveApplication}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot");
            return null;
        }
    }

    public async Task<bool> SaveScreenshotAsync(ScreenshotCapture screenshot, string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var fileName = $"screenshot_{screenshot.Timestamp:yyyyMMdd_HHmmss}.jpg";
            var filePath = Path.Combine(directoryPath, fileName);

            var imageBytes = Convert.FromBase64String(screenshot.Base64Data);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            screenshot.FilePath = filePath;
            _logger.LogDebug($"Screenshot saved to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving screenshot to file");
            return false;
        }
    }

    private Bitmap? CaptureDesktop()
    {
        try
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing desktop");
            return null;
        }
    }

    private static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.First(codec => codec.MimeType == mimeType);
    }
}

public class ActiveWindowInfo
{
    public string ApplicationName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
}

public interface IActiveWindowService
{
    Task<ActiveWindowInfo?> GetActiveWindowInfoAsync();
}

public class ActiveWindowService : IActiveWindowService
{
    private readonly ILogger<ActiveWindowService> _logger;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public ActiveWindowService(ILogger<ActiveWindowService> logger)
    {
        _logger = logger;
    }

    public async Task<ActiveWindowInfo?> GetActiveWindowInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var handle = GetForegroundWindow();
                if (handle == IntPtr.Zero)
                    return null;

                var length = GetWindowTextLength(handle);
                if (length == 0)
                    return null;

                var builder = new StringBuilder(length + 1);
                GetWindowText(handle, builder, builder.Capacity);

                GetWindowThreadProcessId(handle, out uint processId);
                var process = Process.GetProcessById((int)processId);

                return new ActiveWindowInfo
                {
                    WindowTitle = builder.ToString(),
                    ProcessName = process.ProcessName,
                    ProcessId = (int)processId,
                    ApplicationName = GetFriendlyApplicationName(process)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active window info");
                return null;
            }
        });
    }

    private static string GetFriendlyApplicationName(Process process)
    {
        try
        {
            var fileDescription = process.MainModule?.FileVersionInfo.FileDescription;
            if (!string.IsNullOrEmpty(fileDescription))
                return fileDescription;

            var productName = process.MainModule?.FileVersionInfo.ProductName;
            if (!string.IsNullOrEmpty(productName))
                return productName;

            return process.ProcessName;
        }
        catch
        {
            return process.ProcessName;
        }
    }
} 