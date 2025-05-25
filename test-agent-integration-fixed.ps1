# Phase 4: Agent Integration Testing Script
# Test the monitoring agent connection to real-time API

Write-HostWrite-Host "API: https://localhost:7002" -ForegroundColor Cyan
Write-Host "SignalR: https://localhost:7002/hubs/monitoring" -ForegroundColor Cyan============================================" -ForegroundColor Cyan
Write-Host "  Phase 4: Agent Integration Testing" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
Write-Host "[1/5] Checking API Server Status..." -ForegroundColor Yellow
$apiResponse = $null
try {
    $apiResponse = Invoke-RestMethod -Uri "https://localhost:7002/health" -SkipCertificateCheck -TimeoutSec 10
    Write-Host "OK API Server is running on https://localhost:7002" -ForegroundColor Green
} catch {
    Write-Host "ERROR API Server is not responding. Please start it first with: .\start-realtime-system.ps1" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Build the agent
Write-Host "[2/5] Building Monitoring Agent..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build "EmpAnalysis.Agent\EmpAnalysis.Agent.csproj" --configuration Debug --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK Agent built successfully" -ForegroundColor Green
    } else {
        Write-Host "ERROR Agent build failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR Error building agent: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test agent registration endpoint
Write-Host "[3/5] Testing Agent Registration..." -ForegroundColor Yellow
$registrationData = @{
    employeeId = $env:USERNAME
    machineName = $env:COMPUTERNAME
    agentVersion = "1.0.0"
    operatingSystem = [System.Environment]::OSVersion.ToString()
    registrationTime = [System.DateTime]::UtcNow
} | ConvertTo-Json

try {
    $headers = @{ "Content-Type" = "application/json" }
    $registrationResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/register" -Method POST -Body $registrationData -Headers $headers -SkipCertificateCheck
    Write-Host "OK Agent registration successful. Agent ID: $($registrationResponse.agentId)" -ForegroundColor Green
    $agentId = $registrationResponse.agentId
} catch {
    Write-Host "WARNING Agent registration failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This might be expected for first-time setup" -ForegroundColor Yellow
    $agentId = "test-agent-" + (Get-Random)
}

Write-Host ""

# Test heartbeat endpoint
Write-Host "[4/5] Testing Agent Heartbeat..." -ForegroundColor Yellow
$heartbeatData = @{
    agentId = $agentId
    employeeId = $env:USERNAME
    timestamp = [System.DateTime]::UtcNow
    status = "Online"
} | ConvertTo-Json

try {
    $heartbeatResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/agent/heartbeat" -Method POST -Body $heartbeatData -Headers $headers -SkipCertificateCheck
    Write-Host "OK Agent heartbeat successful" -ForegroundColor Green
} catch {
    Write-Host "WARNING Agent heartbeat failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This might be expected for first-time setup" -ForegroundColor Yellow
}

Write-Host ""

# Test monitoring data submission
Write-Host "[5/5] Testing Monitoring Data Submission..." -ForegroundColor Yellow
$monitoringData = @{
    employeeId = $env:USERNAME
    activityType = "Test"
    description = "Phase 4 integration test from PowerShell"
    timestamp = [System.DateTime]::UtcNow
    applicationName = "PowerShell"
    duration = 60
    isProductiveActivity = $true
} | ConvertTo-Json

try {
    $monitoringResponse = Invoke-RestMethod -Uri "https://localhost:7002/api/monitoring/activity" -Method POST -Body $monitoringData -Headers $headers -SkipCertificateCheck
    Write-Host "OK Monitoring data submission successful" -ForegroundColor Green
    Write-Host "   Check the dashboard at https://localhost:8443 for real-time updates!" -ForegroundColor Cyan
} catch {
    Write-Host "ERROR Monitoring data submission failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Phase 4 Integration Test Results" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Start the agent with: .\start-agent.ps1" -ForegroundColor White
Write-Host "  2. Monitor real-time data in dashboard: https://localhost:8443" -ForegroundColor White
Write-Host "  3. Check API logs for agent communication" -ForegroundColor White
Write-Host "  4. Verify SignalR real-time updates" -ForegroundColor White
Write-Host ""
Write-Host "Dashboard: https://localhost:8443" -ForegroundColor Cyan
Write-Host "API: https://localhost:7002" -ForegroundColor Cyan
Write-Host "SignalR: https://localhost:7002/hubs/monitoring" -ForegroundColor Cyan
Write-Host ""
