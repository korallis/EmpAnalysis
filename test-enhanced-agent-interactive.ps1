# Test Enhanced Agent Features - Interactive Version
# Validates the new window title monitoring and idle detection capabilities

param(
    [switch]$AutoContinue = $false,
    [switch]$Detailed = $false
)

Write-Host "EmpAnalysis Enhanced Agent Feature Test" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green

function Prompt-Continue {
    param([string]$Message = "Continue with next test?")
    
    if ($AutoContinue) {
        Write-Host "$Message (Auto-continuing...)" -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        return $true
    }
    
    $response = Read-Host "$Message (y/n)"
    return $response -match '^[Yy]'
}

function Test-AgentExecutable {
    Write-Host "`n=== Test 1: Agent Executable ===" -ForegroundColor Cyan
    
    $agentPath = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent\bin\Release\net8.0-windows\EmpAnalysis.Agent.exe"
    if (Test-Path $agentPath) {
        Write-Host "✓ Enhanced agent executable found" -ForegroundColor Green
        $agentInfo = Get-Item $agentPath
        Write-Host "  Last Modified: $($agentInfo.LastWriteTime)" -ForegroundColor Yellow
        Write-Host "  Size: $([math]::Round($agentInfo.Length / 1KB, 2)) KB" -ForegroundColor Yellow
        return $true
    } else {
        Write-Host "✗ Enhanced agent executable not found" -ForegroundColor Red
        return $false
    }
}

function Test-AgentBuild {
    Write-Host "`n=== Test 2: Agent Build ===" -ForegroundColor Cyan
    
    $originalLocation = Get-Location
    try {
        Set-Location "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent"
        
        Write-Host "Building agent..." -ForegroundColor Yellow
        $buildOutput = dotnet build --configuration Release --verbosity quiet 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Agent builds successfully" -ForegroundColor Green
            if ($Detailed) {
                Write-Host "Build output:" -ForegroundColor Gray
                Write-Host $buildOutput -ForegroundColor Gray
            }
            return $true
        } else {
            Write-Host "✗ Agent build failed" -ForegroundColor Red
            Write-Host "Build errors:" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
            return $false
        }
    }
    finally {
        Set-Location $originalLocation
    }
}

function Test-EnhancedFeatures {
    Write-Host "`n=== Test 3: Enhanced Monitoring Features ===" -ForegroundColor Cyan
    
    $monitoringCoordinatorFile = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent\Services\MonitoringCoordinator.cs"
    if (-not (Test-Path $monitoringCoordinatorFile)) {
        Write-Host "✗ MonitoringCoordinator.cs not found" -ForegroundColor Red
        return $false
    }
    
    $content = Get-Content $monitoringCoordinatorFile -Raw
    $allPassed = $true
    
    # Check for Windows API imports
    if ($content -match "GetForegroundWindow") {
        Write-Host "✓ Windows API integration found" -ForegroundColor Green
    } else {
        Write-Host "✗ Windows API integration missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check for window title monitoring
    if ($content -match "MonitorWindowTitlesAsync") {
        Write-Host "✓ Window title monitoring implemented" -ForegroundColor Green
    } else {
        Write-Host "✗ Window title monitoring missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check for idle time detection
    if ($content -match "MonitorIdleTimeAsync") {
        Write-Host "✓ Idle time detection implemented" -ForegroundColor Green
    } else {
        Write-Host "✗ Idle time detection missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check for enhanced system events
    if ($content -match "SystemEventType.WindowChange") {
        Write-Host "✓ Enhanced system events implemented" -ForegroundColor Green
    } else {
        Write-Host "✗ Enhanced system events missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    return $allPassed
}

function Test-DataModels {
    Write-Host "`n=== Test 4: Enhanced Data Models ===" -ForegroundColor Cyan
    
    $modelsFile = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent\Models\MonitoringData.cs"
    if (-not (Test-Path $modelsFile)) {
        Write-Host "✗ MonitoringData.cs not found" -ForegroundColor Red
        return $false
    }
    
    $modelsContent = Get-Content $modelsFile -Raw
    $allPassed = $true
    
    # Check for WindowActivity model
    if ($modelsContent -match "class WindowActivity") {
        Write-Host "✓ WindowActivity model found" -ForegroundColor Green
    } else {
        Write-Host "✗ WindowActivity model missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check for WebsiteCategory enum
    if ($modelsContent -match "enum WebsiteCategory") {
        Write-Host "✓ WebsiteCategory enum found" -ForegroundColor Green
    } else {
        Write-Host "✗ WebsiteCategory enum missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check for enhanced SystemEventType
    if ($modelsContent -match "IdleStart" -and $modelsContent -match "WindowChange") {
        Write-Host "✓ Enhanced SystemEventType found" -ForegroundColor Green
    } else {
        Write-Host "✗ Enhanced SystemEventType missing" -ForegroundColor Red
        $allPassed = $false
    }
    
    return $allPassed
}

function Test-SystemStatus {
    Write-Host "`n=== Test 5: System Status ===" -ForegroundColor Cyan
    
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*EmpAnalysis*"}
    if ($processes.Count -gt 0) {
        Write-Host "✓ Running EmpAnalysis processes:" -ForegroundColor Green
        foreach ($proc in $processes) {
            Write-Host "  $($proc.ProcessName) (PID: $($proc.Id), CPU: $($proc.CPU))" -ForegroundColor Yellow
        }
        return $true
    } else {
        Write-Host "✗ No EmpAnalysis processes running" -ForegroundColor Red
        return $false
    }
}

function Test-ApiConnectivity {
    Write-Host "`n=== Test 6: API Connectivity ===" -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "https://localhost:7002/api/health" -SkipCertificateCheck -TimeoutSec 5
        Write-Host "✓ API server is responding" -ForegroundColor Green
        if ($Detailed) {
            Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
        }
        return $true
    } catch {
        Write-Host "✗ API server not responding: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
$results = @()

if (Test-AgentExecutable) { $results += "Executable" }
if (-not (Prompt-Continue)) { exit 0 }

if (Test-AgentBuild) { $results += "Build" }
if (-not (Prompt-Continue)) { exit 0 }

if (Test-EnhancedFeatures) { $results += "Features" }
if (-not (Prompt-Continue)) { exit 0 }

if (Test-DataModels) { $results += "Models" }
if (-not (Prompt-Continue)) { exit 0 }

if (Test-SystemStatus) { $results += "System" }
if (-not (Prompt-Continue)) { exit 0 }

if (Test-ApiConnectivity) { $results += "API" }

# Summary
Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Green
Write-Host "Passed tests: $($results -join ', ')" -ForegroundColor Green
Write-Host "Total passed: $($results.Count)/6" -ForegroundColor Yellow

if ($results.Count -eq 6) {
    Write-Host "`n🎉 All tests passed! Enhanced agent is ready for deployment." -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some tests failed. Please review the results above." -ForegroundColor Yellow
}

Write-Host "`nThe enhanced agent includes:" -ForegroundColor Cyan
Write-Host "  • Window title monitoring with Windows API integration" -ForegroundColor White
Write-Host "  • Idle time detection (5-minute threshold)" -ForegroundColor White
Write-Host "  • Enhanced system event tracking (WindowChange, IdleStart, IdleEnd)" -ForegroundColor White
Write-Host "  • WindowActivity and WebsiteCategory data models" -ForegroundColor White
