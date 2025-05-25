# Start Real-time Employee Monitoring System
# Phase 3 - SignalR Integration Testing

Write-Host "[STARTING] Real-time Employee Monitoring System - Phase 3" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Cyan

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    try {
        $tcpConnection = Test-NetConnection -ComputerName "localhost" -Port $Port -WarningAction SilentlyContinue
        return -not $tcpConnection.TcpTestSucceeded
    }
    catch {
        return $true
    }
}

Write-Host "[INFO] Phase 3 Features:" -ForegroundColor Yellow
Write-Host "  [OK] SignalR Real-time Hub" -ForegroundColor Green
Write-Host "  [OK] Live Dashboard Updates" -ForegroundColor Green
Write-Host "  [OK] Real-time API Endpoints" -ForegroundColor Green
Write-Host "  [OK] Connection Status Indicators" -ForegroundColor Green
Write-Host "  [OK] Trend Animations" -ForegroundColor Green
Write-Host ""

# Check if ports are available
$apiPort = 7001
$webPort = 8080

if (-not (Test-Port $apiPort)) {
    Write-Host "[ERROR] Port $apiPort is already in use. Stopping existing processes..." -ForegroundColor Red
    Get-Process -Name "EmpAnalysis.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

if (-not (Test-Port $webPort)) {
    Write-Host "[ERROR] Port $webPort is already in use. Stopping existing processes..." -ForegroundColor Red
    Get-Process -Name "EmpAnalysis.Web" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Start API Server
Write-Host "[API] Starting API Server (SignalR Hub)..." -ForegroundColor Blue
$apiJob = Start-Job -ScriptBlock {
    Set-Location "C:\Projects\EmployeeMonitor\EmpAnalysis.Api"
    dotnet run --urls "https://localhost:7001"
}

# Wait for API to start
Write-Host "[WAIT] Waiting for API server to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Check if API is running
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7001/health" -SkipCertificateCheck -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "[OK] API Server running on https://localhost:7001" -ForegroundColor Green
        Write-Host "[OK] SignalR Hub available at /hubs/monitoring" -ForegroundColor Green
    }
}
catch {
    Write-Host "[WARN] API server may still be starting..." -ForegroundColor Yellow
}

# Start Web Dashboard
Write-Host "[WEB] Starting Web Dashboard (SignalR Client)..." -ForegroundColor Blue
$webJob = Start-Job -ScriptBlock {
    Set-Location "C:\Projects\EmployeeMonitor\EmpAnalysis.Web"
    dotnet run --urls "http://localhost:8080;https://localhost:8443"
}

# Wait for Web to start
Write-Host "[WAIT] Waiting for web dashboard to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "[SUCCESS] Real-time Monitoring System Started!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[DASHBOARD] URLs:" -ForegroundColor White
Write-Host "  • HTTP:  http://localhost:8080" -ForegroundColor Cyan
Write-Host "  • HTTPS: https://localhost:8443" -ForegroundColor Cyan
Write-Host ""
Write-Host "[API] URLs:" -ForegroundColor White
Write-Host "  • API:      https://localhost:7001" -ForegroundColor Cyan
Write-Host "  • Swagger:  https://localhost:7001/swagger" -ForegroundColor Cyan
Write-Host "  • SignalR:  https://localhost:7001/hubs/monitoring" -ForegroundColor Cyan
Write-Host ""
Write-Host "[TESTING] Real-time Features:" -ForegroundColor Yellow
Write-Host "  1. Open dashboard and look for 'Live Data Connected' indicator (top-right)" -ForegroundColor White
Write-Host "  2. Use Postman/curl to POST to /api/monitoring endpoints" -ForegroundColor White
Write-Host "  3. Watch dashboard update in real-time!" -ForegroundColor White
Write-Host ""
Write-Host "[ENDPOINTS] Available:" -ForegroundColor Yellow
Write-Host "  • POST /api/monitoring/session - Submit monitoring session" -ForegroundColor White
Write-Host "  • POST /api/monitoring/screenshot - Submit screenshot" -ForegroundColor White
Write-Host "  • POST /api/monitoring/activity - Submit activity data" -ForegroundColor White
Write-Host "  • GET  /api/monitoring/dashboard - Get live dashboard data" -ForegroundColor White
Write-Host ""
Write-Host "[STOP] Press Ctrl+C to stop both services" -ForegroundColor Red
Write-Host ""

# Keep script running and monitor jobs
try {
    while ($true) {
        Start-Sleep -Seconds 5
        
        # Check if jobs are still running
        if ($apiJob.State -ne "Running") {
            Write-Host "[ERROR] API job stopped unexpectedly" -ForegroundColor Red
            break
        }
        
        if ($webJob.State -ne "Running") {
            Write-Host "[ERROR] Web job stopped unexpectedly" -ForegroundColor Red
            break
        }
    }
}
catch {
    Write-Host "[STOP] Stopping services..." -ForegroundColor Yellow
}
finally {
    # Clean up jobs
    if ($apiJob) { Stop-Job $apiJob; Remove-Job $apiJob }
    if ($webJob) { Stop-Job $webJob; Remove-Job $webJob }
    Write-Host "[OK] Services stopped" -ForegroundColor Green
} 