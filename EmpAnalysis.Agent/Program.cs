using EmpAnalysis.Agent.Configuration;
using EmpAnalysis.Agent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmpAnalysis.Agent;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main(string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configuration
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            // Configure settings
            builder.Services.Configure<AgentSettings>(builder.Configuration);

            // Configure logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventLog(); // For Windows Event Log
            });

            // Register services
            builder.Services.AddHttpClient<IApiCommunicationService, ApiCommunicationService>();
            builder.Services.AddSingleton<IActiveWindowService, ActiveWindowService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IActivityMonitoringService, ActivityMonitoringService>();
            builder.Services.AddSingleton<IProductivityAnalyzer, ProductivityAnalyzer>();
            builder.Services.AddHttpClient<IAttendanceService, AttendanceService>();
            
            // Register the main coordinator as a hosted service
            builder.Services.AddHostedService<MonitoringCoordinator>();

            // Configure as Windows Service if running as service
            if (args.Contains("--service"))
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "EmpAnalysis Agent";
                });
            }

            var host = builder.Build();

            // Check if running in console mode or as service
            if (args.Contains("--console") || Environment.UserInteractive)
            {
                Console.WriteLine("EmpAnalysis Agent starting in console mode...");
                Console.WriteLine("Press Ctrl+C to stop the agent.");
                
                await host.RunAsync();
            }
            else
            {
                // Running as Windows Service
                await host.RunAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error starting EmpAnalysis Agent: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }    
}