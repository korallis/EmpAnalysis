using EmpAnalysis.Web.Components;
using EmpAnalysis.Web.Services;
using EmpAnalysis.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework
builder.Services.AddDbContext<EmpAnalysisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Server=(localdb)\\mssqllocaldb;Database=EmpAnalysisDb;Trusted_Connection=true;MultipleActiveResultSets=true"));

// Add Dashboard Service
builder.Services.AddScoped<DashboardService>();

// Add Data Seed Service
builder.Services.AddScoped<DataSeedService>();

// Add SignalR Service for real-time updates
builder.Services.AddSingleton<SignalRService>();

// Configure HttpClient for API calls
builder.Services.AddHttpClient();

builder.Services.AddScoped<HttpClient>(sp => 
{
    var httpClient = new HttpClient();
    // Set API base address (adjust port as needed)
    httpClient.BaseAddress = new Uri("https://localhost:7001/");
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    return httpClient;
});

// Add Authentication and Authorization services
builder.Services.AddAuthentication()
    .AddCookie("Cookies");
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database is created and seed sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmpAnalysisDbContext>();
    context.Database.EnsureCreated();
    
    // Seed sample data for demonstration
    var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
    await seedService.SeedSampleDataAsync();
}

// Start SignalR service for real-time updates
var signalRService = app.Services.GetRequiredService<SignalRService>();
_ = Task.Run(async () => 
{
    await Task.Delay(5000); // Wait 5 seconds for the app to fully start
    await signalRService.StartAsync();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
