# EmpAnalysis IIS Deployment Script
# This script deploys the EmpAnalysis application to IIS for lb-tech.co.uk

param(
    [string]$SiteName = "EmpAnalysis",
    [string]$Domain = "lb-tech.co.uk",
    [string]$AppPoolName = "EmpAnalysisPool",
    [string]$PublishPath = "C:\inetpub\wwwroot\empanalysis",
    [string]$Port = "80"
)

Write-Host "🚀 Starting EmpAnalysis IIS Deployment..." -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

try {
    # Stop existing applications if running
    Write-Host "📋 Stopping existing applications..." -ForegroundColor Yellow
    Get-Process -Name "EmpAnalysis.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
    Get-Process -Name "EmpAnalysis.Web" -ErrorAction SilentlyContinue | Stop-Process -Force

    # Build the solution in Release mode
    Write-Host "🔨 Building EmpAnalysis solution in Release mode..." -ForegroundColor Yellow
    dotnet build EmpAnalysis.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed!"
    }

    # Publish the Web application
    Write-Host "📦 Publishing Web application..." -ForegroundColor Yellow
    dotnet publish EmpAnalysis.Web/EmpAnalysis.Web.csproj --configuration Release --output "$PublishPath\web" --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Web publish failed!"
    }

    # Publish the API application
    Write-Host "📦 Publishing API application..." -ForegroundColor Yellow
    dotnet publish EmpAnalysis.Api/EmpAnalysis.Api.csproj --configuration Release --output "$PublishPath\api" --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "API publish failed!"
    }

    # Import WebAdministration module
    Import-Module WebAdministration -ErrorAction Stop

    # Create Application Pool
    Write-Host "🏊 Creating Application Pool: $AppPoolName..." -ForegroundColor Yellow
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $AppPoolName
    }
    
    New-WebAppPool -Name $AppPoolName
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.loadUserProfile" -Value $true
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.setProfileEnvironment" -Value $true

    # Remove existing website if it exists
    Write-Host "🌐 Configuring IIS Website: $SiteName..." -ForegroundColor Yellow
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $SiteName
    }

    # Create the main website for the Web application
    New-Website -Name $SiteName -Port $Port -PhysicalPath "$PublishPath\web" -ApplicationPool $AppPoolName
    
    # Add domain binding
    if ($Domain -ne "localhost") {
        New-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader $Domain
        New-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader "www.$Domain"
    }

    # Create API application under the main site
    Write-Host "🔌 Creating API application..." -ForegroundColor Yellow
    New-WebApplication -Site $SiteName -Name "api" -PhysicalPath "$PublishPath\api" -ApplicationPool $AppPoolName

    # Set permissions
    Write-Host "🔐 Setting permissions..." -ForegroundColor Yellow
    $acl = Get-Acl $PublishPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $PublishPath -AclObject $acl

    # Create logs directory
    $logsPath = "$PublishPath\logs"
    if (!(Test-Path $logsPath)) {
        New-Item -ItemType Directory -Path $logsPath -Force
    }
    
    # Set permissions for logs directory
    $acl = Get-Acl $logsPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $logsPath -AclObject $acl

    # Update appsettings for production
    Write-Host "⚙️ Updating production configuration..." -ForegroundColor Yellow
    
    # Update Web appsettings
    $webConfig = Get-Content "$PublishPath\web\appsettings.json" | ConvertFrom-Json
    $webConfig.ApiSettings.BaseUrl = "https://$Domain/api"
    $webConfig.AllowedHosts = "$Domain;*.$Domain;localhost"
    $webConfig | ConvertTo-Json -Depth 10 | Set-Content "$PublishPath\web\appsettings.json"

    # Update API appsettings
    $apiConfig = Get-Content "$PublishPath\api\appsettings.json" | ConvertFrom-Json
    $apiConfig.AllowedHosts = "$Domain;*.$Domain;localhost"
    $apiConfig.ConnectionStrings.DefaultConnection = "Server=localhost\SQLEXPRESS;Database=EmpAnalysis;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
    $apiConfig | ConvertTo-Json -Depth 10 | Set-Content "$PublishPath\api\appsettings.json"

    # Start Application Pool
    Write-Host "▶️ Starting Application Pool..." -ForegroundColor Yellow
    Start-WebAppPool -Name $AppPoolName

    # Test the deployment
    Write-Host "🧪 Testing deployment..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ Web application is responding!" -ForegroundColor Green
        }
    } catch {
        Write-Warning "⚠️ Web application test failed: $($_.Exception.Message)"
    }

    try {
        $response = Invoke-WebRequest -Uri "http://localhost/api/health" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ API is responding!" -ForegroundColor Green
        }
    } catch {
        Write-Warning "⚠️ API test failed: $($_.Exception.Message)"
    }

    Write-Host "🎉 EmpAnalysis deployment completed successfully!" -ForegroundColor Green
    Write-Host "📍 Website: http://$Domain" -ForegroundColor Cyan
    Write-Host "📍 API: http://$Domain/api" -ForegroundColor Cyan
    Write-Host "📍 API Health: http://$Domain/api/health" -ForegroundColor Cyan
    Write-Host "📍 Swagger: http://$Domain/api" -ForegroundColor Cyan
    
    Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Configure DNS A records for $Domain to point to this server" -ForegroundColor White
    Write-Host "2. Run setup-letsencrypt.ps1 to configure SSL certificates" -ForegroundColor White
    Write-Host "3. Update firewall rules to allow HTTP (80) and HTTPS (443)" -ForegroundColor White

} catch {
    Write-Error "❌ Deployment failed: $($_.Exception.Message)"
    Write-Host "📋 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Ensure you're running as Administrator" -ForegroundColor White
    Write-Host "2. Check that IIS is installed and running" -ForegroundColor White
    Write-Host "3. Verify .NET 8 Hosting Bundle is installed" -ForegroundColor White
    Write-Host "4. Check Windows Event Logs for detailed errors" -ForegroundColor White
    exit 1
} 