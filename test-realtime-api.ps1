# Test Real-time API Endpoints
# This script tests the EmpAnalysis real-time monitoring system

Write-Host "[TEST] Starting Real-time API Testing..." -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan

# Function to bypass SSL certificate validation
function Disable-SSLValidation {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint svcPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    }
}

# Enable SSL bypass
Disable-SSLValidation

# Test 1: Dashboard Data Endpoint
Write-Host "[TEST 1] Testing Dashboard Data Endpoint..." -ForegroundColor Blue
try {
    $dashboardResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/monitoring/dashboard" -Method GET -ContentType "application/json"
    Write-Host "✅ Dashboard API: SUCCESS" -ForegroundColor Green
    Write-Host "   Active Employees: $($dashboardResponse.activeEmployees)" -ForegroundColor White
    Write-Host "   Productive Hours: $($dashboardResponse.productiveHours)" -ForegroundColor White
    Write-Host "   Screenshots: $($dashboardResponse.screenshots)" -ForegroundColor White
} catch {
    Write-Host "❌ Dashboard API: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Submit Session Data
Write-Host "[TEST 2] Submitting Session Data..." -ForegroundColor Blue
$sessionData = @{
    employeeId = "test-employee-$(Get-Date -Format 'HHmmss')"
    sessionStart = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    workstationInfo = "PowerShell Test Workstation"
    ipAddress = "192.168.1.100"
    macAddress = "00:11:22:33:44:55"
    operatingSystem = "Windows 11 Pro"
    sessionType = "Testing Session"
    department = "IT Testing"
} | ConvertTo-Json -Depth 10

try {
    $sessionResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/monitoring/session" -Method POST -Body $sessionData -ContentType "application/json"
    Write-Host "✅ Session API: SUCCESS" -ForegroundColor Green
    Write-Host "   Response: $sessionResponse" -ForegroundColor White
} catch {
    Write-Host "❌ Session API: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Submit Activity Data
Write-Host "[TEST 3] Submitting Activity Data..." -ForegroundColor Blue
$activityData = @{
    employeeId = "test-employee-$(Get-Date -Format 'HHmmss')"
    activityType = "ApplicationUsage"
    description = "Testing PowerShell API integration - $(Get-Date -Format 'HH:mm:ss')"
    timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    applicationName = "PowerShell"
    duration = 300
    isProductiveActivity = $true
} | ConvertTo-Json -Depth 10

try {
    $activityResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/monitoring/activity" -Method POST -Body $activityData -ContentType "application/json"
    Write-Host "✅ Activity API: SUCCESS" -ForegroundColor Green
    Write-Host "   Response: $activityResponse" -ForegroundColor White
} catch {
    Write-Host "❌ Activity API: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Submit Screenshot Data
Write-Host "[TEST 4] Submitting Screenshot Data..." -ForegroundColor Blue
$screenshotData = @{
    employeeId = "test-employee-$(Get-Date -Format 'HHmmss')"
    imageData = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="
    capturedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    windowTitle = "PowerShell Test - Real-time API Testing"
    applicationName = "PowerShell ISE"
    isBlurred = $false
} | ConvertTo-Json -Depth 10

try {
    $screenshotResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/monitoring/screenshot" -Method POST -Body $screenshotData -ContentType "application/json"
    Write-Host "✅ Screenshot API: SUCCESS" -ForegroundColor Green
    Write-Host "   Response: $screenshotResponse" -ForegroundColor White
} catch {
    Write-Host "❌ Screenshot API: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "[COMPLETE] Real-time API Testing Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[NEXT] Check your browser dashboard for real-time updates:" -ForegroundColor Yellow
Write-Host "  • Look for 'Live Data Connected' indicator (green)" -ForegroundColor White
Write-Host "  • Watch for metric changes in dashboard cards" -ForegroundColor White
Write-Host "  • Check activity feed for new entries" -ForegroundColor White
Write-Host "  • Observe chart updates with new data points" -ForegroundColor White
Write-Host "" 