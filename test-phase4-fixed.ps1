# Phase 4: Fixed Integration Test (Chat-Safe)
# Fixes issues that were causing chat crashes

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Phase 4: Fixed Integration Testing" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Function to safely test endpoints
function Test-EndpointSafely {
    param($Url, $Method = "GET", $Body = $null)
    
    try {
        $request = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = 5
            UseBasicParsing = $true
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $request.Body = $Body
            $request.ContentType = "application/json"
        }
        
        # Skip certificate validation safely
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
        
        $response = Invoke-WebRequest @request
        return @{ Success = $true; StatusCode = $response.StatusCode }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message.Split("`n")[0] }
    }
}

# Test 1: API Health
Write-Host "[1/4] Testing API Health..." -ForegroundColor Yellow
$healthTest = Test-EndpointSafely -Url "https://localhost:7002/health"
if ($healthTest.Success) {
    Write-Host "✅ API Health: OK (Status: $($healthTest.StatusCode))" -ForegroundColor Green
} else {
    Write-Host "❌ API Health: Failed" -ForegroundColor Red
    Write-Host "   Error: $($healthTest.Error)" -ForegroundColor Yellow
}

Write-Host ""

# Test 2: Agent Registration (simplified)
Write-Host "[2/4] Testing Agent Registration..." -ForegroundColor Yellow
$registrationData = '{"employeeId":"testuser","machineName":"testmachine","agentVersion":"1.0.0"}'
$regTest = Test-EndpointSafely -Url "https://localhost:7002/api/agent/register" -Method "POST" -Body $registrationData

if ($regTest.Success) {
    Write-Host "✅ Agent Registration: OK" -ForegroundColor Green
} else {
    Write-Host "❌ Agent Registration: Failed" -ForegroundColor Red
}

Write-Host ""

# Test 3: Process Check
Write-Host "[3/4] Checking Processes..." -ForegroundColor Yellow
$processes = @{
    "API" = Get-Process | Where-Object {$_.ProcessName -like "*EmpAnalysis.Api*"}
    "Agent" = Get-Process | Where-Object {$_.ProcessName -like "*EmpAnalysis.Agent*"}
    "Web" = Get-Process | Where-Object {$_.ProcessName -like "*EmpAnalysis.Web*"}
}

foreach ($proc in $processes.GetEnumerator()) {
    if ($proc.Value) {
        Write-Host "✅ $($proc.Key): Running (PID: $($proc.Value.Id))" -ForegroundColor Green
    } else {
        Write-Host "❌ $($proc.Key): Not Running" -ForegroundColor Red
    }
}

Write-Host ""

# Test 4: Configuration Check
Write-Host "[4/4] Checking Configuration..." -ForegroundColor Yellow
try {
    $agentConfig = Get-Content "c:\Projects\EmployeeMonitor\EmpAnalysis.Agent\appsettings.json" | ConvertFrom-Json
    $expectedUrl = "https://localhost:7002/api"
    if ($agentConfig.ApiSettings.BaseUrl -eq $expectedUrl) {
        Write-Host "✅ Agent Config: Correct API URL" -ForegroundColor Green
    } else {
        Write-Host "❌ Agent Config: Wrong API URL ($($agentConfig.ApiSettings.BaseUrl))" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Agent Config: Cannot read" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Test Complete - Chat Safe!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
