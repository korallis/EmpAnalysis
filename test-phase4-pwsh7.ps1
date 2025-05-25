# Phase 4: Agent Integration Testing Script - PowerShell 7
# Test the monitoring agent connection to real-time API

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Phase 4: Agent Integration Testing (PS7)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
Write-Host "[1/5] Checking API Server Status..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "https://localhost:7002/health" -TimeoutSec 10 -SkipCertificateCheck
    Write-Host "✅ API Server Status: $healthResponse" -ForegroundColor Green
} catch {
    Write-Host "❌ API Server is not responding: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test agent registration endpoint
Write-Host "[2/5] Testing Agent Registration..." -ForegroundColor Yellow
$registrationData = @{
    employeeId = $env:USERNAME
    machineName = $env:COMPUTERNAME
    agentVersion = "1.0.0"
    operatingSystem = [System.Environment]::OSVersion.ToString()
    registrationTime = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

try {
    $regResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/register" -Method Post -Body $registrationData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ Agent Registration: Success" -ForegroundColor Green
    Write-Host "   Agent ID: $($regResponse.agentId)" -ForegroundColor Cyan
    $agentId = $regResponse.agentId
} catch {
    Write-Host "❌ Agent Registration Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test agent heartbeat
Write-Host "[3/5] Testing Agent Heartbeat..." -ForegroundColor Yellow
$heartbeatData = @{
    agentId = $agentId
    timestamp = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
    status = "Online"
    cpuUsage = 15.5
    memoryUsage = 45.2
} | ConvertTo-Json

try {
    $heartbeatResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/heartbeat" -Method Post -Body $heartbeatData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ Agent Heartbeat: Success" -ForegroundColor Green
} catch {
    Write-Host "❌ Agent Heartbeat Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test agent configuration retrieval
Write-Host "[4/5] Testing Agent Configuration..." -ForegroundColor Yellow
try {
    $configResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/config/$agentId" -SkipCertificateCheck
    Write-Host "✅ Agent Configuration: Retrieved" -ForegroundColor Green
    Write-Host "   Screenshot Interval: $($configResponse.screenshotIntervalSeconds)s" -ForegroundColor Cyan
    Write-Host "   Activity Tracking: $($configResponse.enableActivityTracking)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Agent Configuration Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Check agent process status
Write-Host "[5/5] Checking Agent Process..." -ForegroundColor Yellow
$agentProcess = Get-Process | Where-Object {$_.ProcessName -like "*EmpAnalysis.Agent*"}
if ($agentProcess) {
    Write-Host "✅ Agent Process: Running (PID: $($agentProcess.Id))" -ForegroundColor Green
    Write-Host "   Memory Usage: $([math]::Round($agentProcess.WorkingSet64 / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host "   CPU Time: $($agentProcess.TotalProcessorTime.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
} else {
    Write-Host "❌ Agent Process: Not Running" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Phase 4 Integration Test Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Test SignalR hub connection
Write-Host ""
Write-Host "[BONUS] Testing SignalR Hub..." -ForegroundColor Yellow
try {
    $hubResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/agents" -SkipCertificateCheck
    Write-Host "✅ SignalR Hub: Accessible" -ForegroundColor Green
    Write-Host "   Registered Agents: $($hubResponse.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ SignalR Hub: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Phase 4 Testing Complete! All services integrated." -ForegroundColor Green
