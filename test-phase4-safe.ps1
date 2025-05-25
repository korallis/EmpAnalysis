# Safe Phase 4 Integration Test - Chat-Friendly Version
# Simplified testing to avoid chat crashes

param(
    [switch]$Verbose = $false
)

Write-Host "Phase 4: Safe Integration Test" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# Simple process check
$apiProcess = Get-Process | Where-Object {$_.ProcessName -eq "EmpAnalysis.Api"}
$agentProcess = Get-Process | Where-Object {$_.ProcessName -eq "EmpAnalysis.Agent"}
$webProcess = Get-Process | Where-Object {$_.ProcessName -eq "EmpAnalysis.Web"}

Write-Host ""
Write-Host "Process Status:" -ForegroundColor Yellow
Write-Host "  API Server: $(if($apiProcess) {'Running'} else {'Not Running'})" -ForegroundColor $(if($apiProcess) {'Green'} else {'Red'})
Write-Host "  Agent: $(if($agentProcess) {'Running'} else {'Not Running'})" -ForegroundColor $(if($agentProcess) {'Green'} else {'Red'})
Write-Host "  Web Dashboard: $(if($webProcess) {'Running'} else {'Not Running'})" -ForegroundColor $(if($webProcess) {'Green'} else {'Red'})

# Simple port check
Write-Host ""
Write-Host "Port Status:" -ForegroundColor Yellow
$port7002 = Test-NetConnection -ComputerName localhost -Port 7002 -InformationLevel Quiet
$port8443 = Test-NetConnection -ComputerName localhost -Port 8443 -InformationLevel Quiet
Write-Host "  Port 7002 (API): $(if($port7002) {'Open'} else {'Closed'})" -ForegroundColor $(if($port7002) {'Green'} else {'Red'})
Write-Host "  Port 8443 (Web): $(if($port8443) {'Open'} else {'Closed'})" -ForegroundColor $(if($port8443) {'Green'} else {'Red'})

# Configuration check
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
$agentConfig = Get-Content "c:\Projects\EmployeeMonitor\EmpAnalysis.Agent\appsettings.json" | ConvertFrom-Json
$webConfig = Get-Content "c:\Projects\EmployeeMonitor\EmpAnalysis.Web\appsettings.json" | ConvertFrom-Json
Write-Host "  Agent API URL: $($agentConfig.ApiSettings.BaseUrl)" -ForegroundColor White
Write-Host "  Web API URL: $($webConfig.ApiSettings.BaseUrl)" -ForegroundColor White

# Simple connectivity test (without output)
Write-Host ""
Write-Host "Connectivity Test:" -ForegroundColor Yellow
try {
    $null = Invoke-WebRequest -Uri "https://localhost:7002/health" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Write-Host "  API Health: OK" -ForegroundColor Green
} catch {
    Write-Host "  API Health: Failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test Complete - No crashes!" -ForegroundColor Green
