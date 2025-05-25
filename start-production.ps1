# EmpAnalysis Production Startup Script
# Starts the application bound to all network interfaces for lb-tech.co.uk

Write-Host "🚀 Starting EmpAnalysis in Production Mode..." -ForegroundColor Green
Write-Host "Domain: lb-tech.co.uk" -ForegroundColor Yellow
Write-Host "HTTP Port: 8080" -ForegroundColor Yellow
Write-Host "HTTPS Port: 8443" -ForegroundColor Yellow
Write-Host ""

# Set environment to Production
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Navigate to Web project
Set-Location -Path "EmpAnalysis.Web"

# Start the application
Write-Host "Starting Kestrel server..." -ForegroundColor Green
dotnet run --configuration Release --urls "http://0.0.0.0:8080;https://0.0.0.0:8443" 