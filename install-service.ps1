# Install EmpAnalysis as Windows Service
# Requires Administrator privileges

Write-Host "🔧 Installing EmpAnalysis as Windows Service..." -ForegroundColor Green

# Service configuration
$serviceName = "EmpAnalysis"
$serviceDisplayName = "EmpAnalysis Employee Monitoring"
$serviceDescription = "EmpAnalysis Employee Monitoring Application for lb-tech.co.uk"
$exePath = "$PWD\EmpAnalysis.Web\bin\Release\net8.0\EmpAnalysis.Web.exe"
$workingDirectory = "$PWD\EmpAnalysis.Web"

# Check if service already exists
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "⚠️ Service already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
}

# Build the application in Release mode
Write-Host "🔨 Building application in Release mode..." -ForegroundColor Yellow
dotnet publish EmpAnalysis.Web -c Release --self-contained false

# Create the service
Write-Host "📦 Creating Windows Service..." -ForegroundColor Yellow
sc.exe create $serviceName binPath= "`"$exePath`" --urls `"http://0.0.0.0:8080;https://0.0.0.0:8443`"" start= auto DisplayName= $serviceDisplayName

# Set service description
sc.exe description $serviceName $serviceDescription

# Configure service to restart on failure
sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/5000/restart/5000

# Set service to run as Network Service (more secure than Local System)
sc.exe config $serviceName obj= "NT AUTHORITY\NetworkService"

Write-Host "✅ Service installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To start the service: Start-Service -Name '$serviceName'" -ForegroundColor White
Write-Host "To check status: Get-Service -Name '$serviceName'" -ForegroundColor White
Write-Host "To view logs: Get-EventLog -LogName Application -Source '$serviceName'" -ForegroundColor White 