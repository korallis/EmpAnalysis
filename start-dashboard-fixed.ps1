#!/usr/bin/env pwsh
# EmpAnalysis Dashboard Startup Script (Fixed Version)
# This script uses --project parameter to work from any directory

Write-Host "🚀 Starting EmpAnalysis Dashboard..." -ForegroundColor Green
Write-Host ""

# Use --project parameter to ensure it works from any directory
try {
    Write-Host "Building and starting the application..." -ForegroundColor Yellow
    Write-Host ""
    
    dotnet run --project EmpAnalysis.Web --urls="http://0.0.0.0:8080;https://0.0.0.0:8443"
}
catch {
    Write-Host "❌ Error starting application: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure you're in the EmployeeMonitor project root directory" -ForegroundColor Yellow
    Read-Host "Press any key to continue"
}

Write-Host ""
Write-Host "Application has stopped." -ForegroundColor Yellow
Read-Host "Press any key to exit" 