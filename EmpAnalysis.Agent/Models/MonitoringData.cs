using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Agent.Models;

public class MonitoringSession
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<ApplicationUsage> Applications { get; set; } = new();
    public List<WebsiteVisit> WebsiteVisits { get; set; } = new();
    public List<ScreenshotCapture> Screenshots { get; set; } = new();
    public List<SystemEvent> SystemEvents { get; set; } = new();
}

public class ApplicationUsage
{
    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationPath { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsProductiveApp { get; set; }
    public string Category { get; set; } = "Unknown";
}

public class WebsiteVisit
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsProductiveSite { get; set; }
    public string Category { get; set; } = "Unknown";
}

public class ScreenshotCapture
{
    public DateTime Timestamp { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Base64Data { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public string ActiveApplication { get; set; } = string.Empty;
    public string ActiveWindow { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
}

public class SystemEvent
{
    public DateTime Timestamp { get; set; }
    public SystemEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public enum SystemEventType
{
    Login,
    Logout,
    Lock,
    Unlock,
    Idle,
    Active,
    IdleStart,
    IdleEnd,
    WindowChange,
    ApplicationStart,
    ApplicationEnd,
    FileAccess,
    NetworkActivity,
    USBInsert,
    USBRemove,
    PrintJob
}

public class MonitoringStats
{
    public DateTime Date { get; set; }
    public TimeSpan TotalActiveTime { get; set; }
    public TimeSpan ProductiveTime { get; set; }
    public TimeSpan UnproductiveTime { get; set; }
    public int ApplicationSwitches { get; set; }
    public int WebsiteVisits { get; set; }
    public int ScreenshotsTaken { get; set; }
    public double ProductivityScore { get; set; }
    public List<string> TopApplications { get; set; } = new();
    public List<string> TopWebsites { get; set; } = new();
}

public class WindowActivity
{
    public DateTime Timestamp { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public enum WebsiteCategory
{
    Productive,
    Neutral, 
    Unproductive,
    Blocked,
    Unknown
}