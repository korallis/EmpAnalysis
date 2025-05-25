using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using EmpAnalysis.Shared.Data;
using EmpAnalysis.Shared.Models;

namespace EmpAnalysis.Api.Hubs;

public class MonitoringHub : Hub
{
    private readonly EmpAnalysisDbContext _context;
    private readonly ILogger<MonitoringHub> _logger;

    public MonitoringHub(EmpAnalysisDbContext context, ILogger<MonitoringHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task JoinDashboardGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation("Client {ConnectionId} joined Dashboard group", Context.ConnectionId);
    }

    public async Task LeaveDashboardGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation("Client {ConnectionId} left Dashboard group", Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Send real-time dashboard updates
    public async Task SendDashboardUpdate(object dashboardData)
    {
        await Clients.Group("Dashboard").SendAsync("DashboardUpdate", dashboardData);
    }

    // Send real-time activity update
    public async Task SendActivityUpdate(object activityData)
    {
        await Clients.Group("Dashboard").SendAsync("ActivityUpdate", activityData);
    }

    // Send employee status update
    public async Task SendEmployeeStatusUpdate(object statusData)
    {
        await Clients.Group("Dashboard").SendAsync("EmployeeStatusUpdate", statusData);
    }

    // Send new screenshot notification
    public async Task SendScreenshotUpdate(object screenshotData)
    {
        await Clients.Group("Dashboard").SendAsync("ScreenshotUpdate", screenshotData);
    }

    // Send system alert
    public async Task SendSystemAlert(object alertData)
    {
        await Clients.Group("Dashboard").SendAsync("SystemAlert", alertData);
    }
} 