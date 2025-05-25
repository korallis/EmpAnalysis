#!/usr/bin/env pwsh

# Universal Dashboard Startup Script
# This script can be run from the root directory without navigation issues

param(
    [string]$Urls = "http://*:8080;https://*:8443",
    [string]$Project = "web",
    [switch]$Help
)

if ($Help) {
    Write-Host "Universal Dashboard Startup Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\start-dashboard.ps1                    # Start web dashboard"
    Write-Host "  .\start-dashboard.ps1 -Project api       # Start API"
    Write-Host "  .\start-dashboard.ps1 -Project agent     # Start agent"
    Write-Host "  .\start-dashboard.ps1 -Urls 'http://*:9000'  # Custom URLs"
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Project    : web, api, or agent (default: web)"
    Write-Host "  -Urls       : Custom URLs (default: http://*:8080;https://*:8443)"
    Write-Host "  -Help       : Show this help message"
    exit 0
}

# Get the script directory (root of the project)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Define project mappings
$ProjectMappings = @{
    "web" = @{
        "Path" = "EmpAnalysis.Web"
        "Name" = "Employee Monitoring Dashboard"
        "Port" = "8080/8443"
    }
    "api" = @{
        "Path" = "EmpAnalysis.Api" 
        "Name" = "Employee Monitoring API"
        "Port" = "7001/7000"
        "DefaultUrls" = "http://*:7001;https://*:7000"
    }
    "agent" = @{
        "Path" = "EmpAnalysis.Agent"
        "Name" = "Employee Monitoring Agent"
        "Port" = "N/A (Windows Service)"
        "DefaultUrls" = ""
    }
}

# Validate project parameter
if ($ProjectMappings.ContainsKey($Project) -eq $false) {
    Write-Host "❌ Invalid project: $Project" -ForegroundColor Red
    Write-Host "Valid options: web, api, agent" -ForegroundColor Yellow
    exit 1
}

$SelectedProject = $ProjectMappings[$Project]
$ProjectPath = Join-Path $ScriptDir $SelectedProject.Path

# Check if project directory exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "❌ Project directory not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

# Check if project file exists
$ProjectFile = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" | Select-Object -First 1
if (-not $ProjectFile) {
    Write-Host "❌ No .csproj file found in: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "🚀 Starting $($SelectedProject.Name)..." -ForegroundColor Green
Write-Host "📁 Project: $($SelectedProject.Path)" -ForegroundColor Cyan
Write-Host "🌐 Port(s): $($SelectedProject.Port)" -ForegroundColor Cyan

# Set URLs based on project type
if ($Project -eq "api" -and $Urls -eq "http://*:8080;https://*:8443") {
    $Urls = $SelectedProject.DefaultUrls
}

if ($Project -eq "agent") {
    Write-Host "⚠️  Note: Agent runs as a Windows service. Use Ctrl+C to stop." -ForegroundColor Yellow
}

Write-Host "🔗 URLs: $Urls" -ForegroundColor Cyan
Write-Host ""
Write-Host "Starting in 3 seconds... (Ctrl+C to cancel)" -ForegroundColor Yellow

# Countdown
for ($i = 3; $i -gt 0; $i--) {
    Write-Host "⏱️  $i..." -ForegroundColor Gray
    Start-Sleep -Seconds 1
}

try {
    # Change to project directory
    Push-Location $ProjectPath
    
    Write-Host ""
    Write-Host "🏃‍♂️ Running: dotnet run --urls `"$Urls`"" -ForegroundColor Green
    Write-Host "📂 Working Directory: $(Get-Location)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Blue
    
    # Run the project
    if ($Project -eq "agent") {
        dotnet run
    } else {
        dotnet run --urls $Urls
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Always return to original directory
    Pop-Location
    Write-Host ""
    Write-Host "🛑 Application stopped." -ForegroundColor Yellow
} 