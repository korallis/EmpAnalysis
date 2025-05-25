namespace EmpAnalysis.Agent.Configuration;

public class AgentSettings
{
    public ApiSettings ApiSettings { get; set; } = new();
    public MonitoringSettings MonitoringSettings { get; set; } = new();
    public EmployeeSettings EmployeeSettings { get; set; } = new();
    public AgentConfiguration AgentConfiguration { get; set; } = new();
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int RetryAttempts { get; set; } = 3;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public int ConfigurationSyncIntervalMinutes { get; set; } = 10;
    public int OfflineRetryIntervalMinutes { get; set; } = 5;
}

public class MonitoringSettings
{
    public int ScreenshotIntervalSeconds { get; set; } = 300;
    public int ActivityTrackingIntervalSeconds { get; set; } = 30;
    public bool ApplicationUsageTrackingEnabled { get; set; } = true;
    public bool WebsiteTrackingEnabled { get; set; } = true;
    public bool KeystrokeTrackingEnabled { get; set; } = false;
    public bool MouseTrackingEnabled { get; set; } = false;
    public int MaxScreenshotsPerDay { get; set; } = 288;
    public bool CompressScreenshots { get; set; } = true;
    public int ScreenshotQuality { get; set; } = 75;
    public bool RealTimeDataSubmission { get; set; } = true;
    public int BatchSubmissionIntervalMinutes { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 100;
}

public class EmployeeSettings
{
    public string EmployeeId { get; set; } = string.Empty;
    public bool AutoDetectUser { get; set; } = true;
    public WorkingHours WorkingHours { get; set; } = new();
}

public class AgentConfiguration
{
    public string AgentVersion { get; set; } = "1.0.0";
    public bool EnableSelfDiagnostics { get; set; } = true;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public int MaxMemoryUsageMB { get; set; } = 100;
    public int MaxCpuUsagePercent { get; set; } = 10;
    public bool EnableAutoUpdate { get; set; } = false;
    public int DataRetentionDays { get; set; } = 30;
}

public class WorkingHours
{
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public List<DayOfWeek> WorkingDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    public bool IsWorkingTime(DateTime dateTime)
    {
        var time = dateTime.TimeOfDay;
        return WorkingDays.Contains(dateTime.DayOfWeek) &&
               time >= StartTime &&
               time <= EndTime;
    }
} 