# EmpAnalysis IIS Setup Script
# Run as Administrator

Write-Host "=== EmpAnalysis IIS Setup ===" -ForegroundColor Green
Write-Host ""

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click on PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "1. Installing IIS Features..." -ForegroundColor Yellow

# Enable IIS features
$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer", 
    "IIS-CommonHttpFeatures",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-NetFxExtensibility45",
    "IIS-NetFxExtensibility",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-Performance",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole",
    "IIS-IIS6ManagementCompatibility",
    "IIS-Metabase",
    "IIS-ASPNET45"
)

foreach ($feature in $features) {
    try {
        Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -ErrorAction SilentlyContinue
        Write-Host "  ✓ $feature" -ForegroundColor Green
    }
    catch {
        Write-Host "  ⚠ $feature (may already be installed)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "2. Downloading .NET 8 Hosting Bundle..." -ForegroundColor Yellow

# Download .NET 8 Hosting Bundle
$hostingBundleUrl = "https://download.visualstudio.microsoft.com/download/pr/b8cf9416-e2f8-4c12-b2c2-b8eca5835a2d/d3d1af8b47b7bb0d50fedc31fa4e26e1/dotnet-hosting-8.0.12-win.exe"
$hostingBundlePath = "$env:TEMP\dotnet-hosting-8.0.12-win.exe"

try {
    Invoke-WebRequest -Uri $hostingBundleUrl -OutFile $hostingBundlePath
    Write-Host "  ✓ Downloaded hosting bundle" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ Failed to download hosting bundle" -ForegroundColor Red
    Write-Host "Please download manually from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "3. Installing .NET 8 Hosting Bundle..." -ForegroundColor Yellow

if (Test-Path $hostingBundlePath) {
    try {
        Start-Process -FilePath $hostingBundlePath -ArgumentList "/quiet" -Wait
        Write-Host "  ✓ .NET 8 Hosting Bundle installed" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to install hosting bundle" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "4. Configuring Firewall..." -ForegroundColor Yellow

# Configure Windows Firewall
try {
    New-NetFirewallRule -DisplayName "HTTP Inbound" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow -ErrorAction SilentlyContinue
    New-NetFirewallRule -DisplayName "HTTPS Inbound" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow -ErrorAction SilentlyContinue
    Write-Host "  ✓ Firewall rules configured" -ForegroundColor Green
}
catch {
    Write-Host "  ⚠ Firewall rules may already exist" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Restarting IIS..." -ForegroundColor Yellow

try {
    iisreset
    Write-Host "  ✓ IIS restarted" -ForegroundColor Green
}
catch {
    Write-Host "  ⚠ IIS restart may be needed manually" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Build and publish your EmpAnalysis application" -ForegroundColor White
Write-Host "2. Create IIS site pointing to lb-tech.co.uk" -ForegroundColor White
Write-Host "3. Install Let's Encrypt certificate" -ForegroundColor White
Write-Host "4. Configure DNS to point to this server" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 