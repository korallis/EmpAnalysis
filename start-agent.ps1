# EmpAnalysis Monitoring Agent Startup Script
# This script ensures we start from the correct directory to avoid "project not found" errors

Write-Host "🕵️ Starting EmpAnalysis Monitoring Agent..." -ForegroundColor Green
Write-Host ""

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AgentProjectDir = Join-Path $ScriptDir "EmpAnalysis.Agent"

# Check if we're in the correct location
if (-not (Test-Path $AgentProjectDir)) {
    Write-Host "❌ Error: EmpAnalysis.Agent directory not found!" -ForegroundColor Red
    Write-Host "   Make sure this script is in the root EmployeeMonitor directory." -ForegroundColor Yellow
    exit 1
}

# Change to the agent project directory
Set-Location $AgentProjectDir
Write-Host "📁 Changed to directory: $AgentProjectDir" -ForegroundColor Cyan

# Check if the project file exists
if (-not (Test-Path "EmpAnalysis.Agent.csproj")) {
    Write-Host "❌ Error: EmpAnalysis.Agent.csproj not found in current directory!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Project file found: EmpAnalysis.Agent.csproj" -ForegroundColor Green
Write-Host ""

# Check for elevated permissions
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "⚠️  Warning: Running without administrator privileges" -ForegroundColor Yellow
    Write-Host "   Some monitoring features may not work properly." -ForegroundColor Yellow
    Write-Host "   Consider running PowerShell as Administrator for full functionality." -ForegroundColor Yellow
    Write-Host ""
}

# Start the agent in console mode
Write-Host "🔍 Starting monitoring agent in console mode..." -ForegroundColor Yellow
Write-Host "   • Screenshots: Every 5 minutes during work hours" -ForegroundColor Cyan
Write-Host "   • Activity: Every 30 seconds" -ForegroundColor Cyan
Write-Host "   • Data sync: Every 5 minutes" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the agent" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Gray

# Run the agent in console mode
dotnet run --console 