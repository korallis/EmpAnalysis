#!/usr/bin/env pwsh

# Employee Monitoring Dashboard Startup Script
# Robust script that works from any directory

param(
    [string]$Urls = "http://*:8080;https://*:8443",
    [switch]$Help
)

if ($Help) {
    Write-Host "Employee Monitoring Dashboard Startup Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\start-web.ps1                                    # Default URLs"
    Write-Host "  .\start-web.ps1 -Urls 'http://*:9000'               # Custom URLs"
    Write-Host "  .\start-web.ps1 -Help                              # Show this help"
    Write-Host ""
    Write-Host "Default URLs: http://*:8080;https://*:8443" -ForegroundColor Cyan
    exit 0
}

# Get the script directory and find the project
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptDir "EmpAnalysis.Web"

# Validate project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "❌ Project directory not found: $ProjectPath" -ForegroundColor Red
    Write-Host "Make sure you're running this script from the EmployeeMonitor root directory." -ForegroundColor Yellow
    exit 1
}

$ProjectFile = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" | Select-Object -First 1
if (-not $ProjectFile) {
    Write-Host "❌ No .csproj file found in: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "🚀 Starting Employee Monitoring Dashboard..." -ForegroundColor Green
Write-Host "📁 Project: EmpAnalysis.Web" -ForegroundColor Cyan
Write-Host "🔗 URLs: $Urls" -ForegroundColor Cyan
Write-Host ""
Write-Host "🌐 Dashboard will be available at:" -ForegroundColor Yellow
Write-Host "   • http://localhost:8080" -ForegroundColor White
Write-Host "   • https://localhost:8443" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Gray
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Blue

try {
    # Change to project directory
    Push-Location $ProjectPath
    
    Write-Host ""
    Write-Host "🏃‍♂️ Running: dotnet run --urls `"$Urls`"" -ForegroundColor Green
    Write-Host "📂 Working Directory: $(Get-Location)" -ForegroundColor Gray
    Write-Host ""
    
    # Start the web application
    dotnet run --urls $Urls
    
} catch {
    Write-Host ""
    Write-Host "❌ Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Make sure .NET 8 is installed: dotnet --version" -ForegroundColor White
    Write-Host "2. Try building first: dotnet build" -ForegroundColor White
    Write-Host "3. Check if ports are in use: netstat -an | findstr :8080" -ForegroundColor White
    exit 1
} finally {
    # Always return to original directory
    Pop-Location
    Write-Host ""
    Write-Host "🛑 Dashboard stopped." -ForegroundColor Yellow
} 