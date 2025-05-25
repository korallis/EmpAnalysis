# EmpAnalysis Firewall Configuration Script
# Run this script as Administrator

Write-Host "=== EmpAnalysis Firewall Configuration ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator!"
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "✅ Running with Administrator privileges" -ForegroundColor Green
Write-Host ""

# Function to create firewall rule safely
function New-FirewallRuleIfNotExists {
    param(
        [string]$DisplayName,
        [string]$Port,
        [string]$Description
    )
    
    try {
        # Check if rule already exists
        $existingRule = Get-NetFirewallRule -DisplayName $DisplayName -ErrorAction SilentlyContinue
        
        if ($existingRule) {
            Write-Host "⚠️  Firewall rule '$DisplayName' already exists - skipping" -ForegroundColor Yellow
        } else {
            New-NetFirewallRule -DisplayName $DisplayName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -Profile Domain,Private,Public -Description $Description | Out-Null
            Write-Host "✅ Created firewall rule: $DisplayName (Port $Port)" -ForegroundColor Green
        }
    }
    catch {
        Write-Error "❌ Failed to create firewall rule '$DisplayName': $($_.Exception.Message)"
    }
}

Write-Host "Creating Windows Firewall rules for EmpAnalysis..." -ForegroundColor White
Write-Host ""

# Development ports
New-FirewallRuleIfNotExists -DisplayName "EmpAnalysis API - Port 5213" -Port "5213" -Description "Allow inbound traffic for EmpAnalysis API development server"
New-FirewallRuleIfNotExists -DisplayName "EmpAnalysis Web - Port 5214" -Port "5214" -Description "Allow inbound traffic for EmpAnalysis Web development server"

# Production ports
New-FirewallRuleIfNotExists -DisplayName "EmpAnalysis Production HTTP - Port 80" -Port "80" -Description "Allow HTTP traffic for EmpAnalysis production deployment"
New-FirewallRuleIfNotExists -DisplayName "EmpAnalysis Production HTTPS - Port 443" -Port "443" -Description "Allow HTTPS traffic for EmpAnalysis production deployment"

# SQL Server port (if needed for remote database access)
New-FirewallRuleIfNotExists -DisplayName "EmpAnalysis SQL Server - Port 1433" -Port "1433" -Description "Allow SQL Server traffic for EmpAnalysis database"

Write-Host ""
Write-Host "=== Firewall Configuration Summary ===" -ForegroundColor Cyan

# Display current EmpAnalysis firewall rules
Write-Host ""
Write-Host "Current EmpAnalysis firewall rules:" -ForegroundColor White
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*EmpAnalysis*"} | Select-Object DisplayName, Enabled, Direction | Format-Table -AutoSize

# Test current ports
Write-Host ""
Write-Host "Testing port availability:" -ForegroundColor White
$ports = @(5213, 5214, 80, 443, 1433)
foreach ($port in $ports) {
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Write-Host "✅ Port $port is accessible" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Port $port is not currently in use (normal if service not running)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "⚠️  Port $port status unknown" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎉 Firewall configuration completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "1. EmpAnalysis API will be accessible on port 5213" -ForegroundColor Gray
Write-Host "2. EmpAnalysis Web will be accessible on port 5214" -ForegroundColor Gray
Write-Host "3. Production deployment will use ports 80 (HTTP) and 443 (HTTPS)" -ForegroundColor Gray
Write-Host "4. Run your applications and test external access" -ForegroundColor Gray
Write-Host ""

# Display local IP addresses for reference
Write-Host "Your local IP addresses for testing:" -ForegroundColor White
Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -ne "127.0.0.1" -and $_.PrefixOrigin -eq "Dhcp"} | Select-Object IPAddress, InterfaceAlias | Format-Table -AutoSize

pause 