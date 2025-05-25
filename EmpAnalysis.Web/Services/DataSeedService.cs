using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Web.Services;

public class DataSeedService
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<DataSeedService> _logger;

    public DataSeedService(EmpAnalysisDbContext context, ILogger<DataSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedSampleDataAsync()
    {
        try
        {
            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Sample data already exists, skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding sample data...");

            // Create sample employees
            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "sarah.chen",
                    Email = "sarah.chen@company.com",
                    FirstName = "Sarah",
                    LastName = "Chen",
                    Department = "Engineering",
                    JobTitle = "Senior Developer",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddMonths(-18),
                    LastLoginAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "marcus.johnson",
                    Email = "marcus.johnson@company.com",
                    FirstName = "Marcus",
                    LastName = "Johnson",
                    Department = "Design",
                    JobTitle = "UI/UX Designer",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddMonths(-12),
                    LastLoginAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "elena.rodriguez",
                    Email = "elena.rodriguez@company.com",
                    FirstName = "Elena",
                    LastName = "Rodriguez",
                    Department = "Marketing",
                    JobTitle = "Marketing Manager",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddMonths(-24),
                    LastLoginAt = DateTime.UtcNow.AddHours(-2)
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "david.kim",
                    Email = "david.kim@company.com",
                    FirstName = "David",
                    LastName = "Kim",
                    Department = "Sales",
                    JobTitle = "Sales Representative",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddMonths(-8),
                    LastLoginAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "lisa.thompson",
                    Email = "lisa.thompson@company.com",
                    FirstName = "Lisa",
                    LastName = "Thompson",
                    Department = "HR",
                    JobTitle = "HR Specialist",
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddMonths(-15),
                    LastLoginAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(employees);
            await _context.SaveChangesAsync();

            // Create sample application usage data
            var today = DateTime.Today;
            var applications = new List<EmpAnalysis.Shared.Models.ApplicationUsage>();
            var random = new Random();

            foreach (var employee in employees)
            {
                var appNames = new[]
                {
                    "Visual Studio Code", "Google Chrome", "Microsoft Teams", "Figma",
                    "Slack", "Excel", "Zoom", "Notion", "Outlook", "Adobe Photoshop"
                };

                foreach (var appName in appNames.Take(6))
                {
                    var startTime = today.AddHours(9 + random.NextDouble() * 8);
                    var duration = TimeSpan.FromMinutes(random.Next(30, 240));

                    applications.Add(new EmpAnalysis.Shared.Models.ApplicationUsage
                    {
                        EmployeeId = employee.Id,
                        ApplicationName = appName,
                        ExecutablePath = $"C:\\Program Files\\{appName}\\{appName}.exe",
                        WindowTitle = $"{appName} - Active Window",
                        StartTime = startTime,
                        EndTime = startTime.Add(duration),
                        Duration = duration,
                        IsProductiveApplication = IsProductiveApp(appName),
                        Category = GetAppCategory(appName)
                    });
                }
            }

            _context.ApplicationUsages.AddRange(applications);
            await _context.SaveChangesAsync();

            // Create sample website visits
            var websites = new List<WebsiteVisit>();
            var siteUrls = new[]
            {
                "github.com", "stackoverflow.com", "docs.microsoft.com", "linkedin.com",
                "facebook.com", "youtube.com", "news.bbc.co.uk", "figma.com"
            };

            foreach (var employee in employees)
            {
                foreach (var url in siteUrls.Take(4))
                {
                    var startTime = today.AddHours(9 + random.NextDouble() * 8);
                    var duration = TimeSpan.FromMinutes(random.Next(5, 60));

                    websites.Add(new WebsiteVisit
                    {
                        EmployeeId = employee.Id,
                        Url = $"https://{url}",
                        Title = $"{url} - Page Title",
                        Domain = url,
                        VisitStart = startTime,
                        VisitEnd = startTime.Add(duration),
                        Duration = duration,
                        IsProductiveTime = IsProductiveSite(url),
                        Category = GetSiteCategory(url)
                    });
                }
            }

            _context.WebsiteVisits.AddRange(websites);
            await _context.SaveChangesAsync();

            // Create sample screenshots
            var screenshots = new List<Screenshot>();
            foreach (var employee in employees)
            {
                for (int i = 0; i < 15; i++)
                {
                    var captureTime = today.AddHours(9 + (i * 0.5));
                    screenshots.Add(new Screenshot
                    {
                        EmployeeId = employee.Id,
                        CapturedAt = captureTime,
                        FileName = $"screenshot_{captureTime:yyyyMMdd_HHmmss}_{employee.UserName}.jpg",
                        FilePath = $"/screenshots/{employee.Id}/{captureTime:yyyy/MM/dd}/",
                        FileSizeBytes = random.Next(50000, 200000),
                        ActiveApplication = applications.Where(a => a.EmployeeId == employee.Id).OrderBy(_ => random.Next()).First().ApplicationName,
                        WindowTitle = "Active Window Title"
                    });
                }
            }

            _context.Screenshots.AddRange(screenshots);
            await _context.SaveChangesAsync();

            // Create sample activity logs
            var activityLogs = new List<ActivityLog>();
            foreach (var employee in employees)
            {
                // Login activity
                activityLogs.Add(new ActivityLog
                {
                    EmployeeId = employee.Id,
                    ActivityType = ActivityType.Login,
                    Description = "User logged in",
                    Timestamp = today.AddHours(9),
                    ApplicationName = "Windows",
                    Category = "System"
                });

                // Application starts
                foreach (var app in applications.Where(a => a.EmployeeId == employee.Id).Take(3))
                {
                    activityLogs.Add(new ActivityLog
                    {
                        EmployeeId = employee.Id,
                        ActivityType = ActivityType.ApplicationStart,
                        Description = $"Started {app.ApplicationName}",
                        Timestamp = app.StartTime,
                        ApplicationName = app.ApplicationName,
                        Category = app.Category
                    });
                }

                // System events
                if (random.Next(1, 10) > 7) // 30% chance of system event
                {
                    activityLogs.Add(new ActivityLog
                    {
                        EmployeeId = employee.Id,
                        ActivityType = ActivityType.SystemEvent,
                        Description = "System performance warning detected",
                        Timestamp = today.AddHours(10 + random.NextDouble() * 6),
                        Category = "System"
                    });
                }

                // USB device events
                if (random.Next(1, 10) > 8) // 20% chance of USB event
                {
                    activityLogs.Add(new ActivityLog
                    {
                        EmployeeId = employee.Id,
                        ActivityType = ActivityType.UsbDevice,
                        Description = "USB device connected",
                        Timestamp = today.AddHours(11 + random.NextDouble() * 4),
                        Category = "Security"
                    });
                }
            }

            _context.ActivityLogs.AddRange(activityLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sample data seeded successfully! Added {EmployeeCount} employees, {AppCount} application usage records, {WebsiteCount} website visits, {ScreenshotCount} screenshots, and {ActivityCount} activity logs.",
                employees.Count, applications.Count, websites.Count, screenshots.Count, activityLogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample data");
            throw;
        }
    }

    private static bool IsProductiveApp(string appName) => appName switch
    {
        "Visual Studio Code" => true,
        "Figma" => true,
        "Notion" => true,
        "Excel" => true,
        "Outlook" => true,
        "Adobe Photoshop" => true,
        _ => false
    };

    private static bool IsProductiveSite(string url) => url switch
    {
        "github.com" => true,
        "stackoverflow.com" => true,
        "docs.microsoft.com" => true,
        "figma.com" => true,
        _ => false
    };

    private static string GetAppCategory(string appName) => appName switch
    {
        "Visual Studio Code" => "Development",
        "Google Chrome" => "Browser",
        "Microsoft Teams" => "Communication",
        "Figma" => "Design",
        "Slack" => "Communication",
        "Excel" => "Productivity",
        "Zoom" => "Communication",
        "Notion" => "Productivity",
        "Outlook" => "Email",
        "Adobe Photoshop" => "Design",
        _ => "Other"
    };

    private static string GetSiteCategory(string url) => url switch
    {
        "github.com" => "Development",
        "stackoverflow.com" => "Development",
        "docs.microsoft.com" => "Development",
        "linkedin.com" => "Professional",
        "facebook.com" => "Social",
        "youtube.com" => "Entertainment",
        "news.bbc.co.uk" => "News",
        "figma.com" => "Design",
        _ => "Other"
    };
} 