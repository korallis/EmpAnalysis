# Simple Let's Encrypt SSL Setup for EmpAnalysis
# Requires Administrator privileges

Write-Host "🔐 Setting up Let's Encrypt SSL for lb-tech.co.uk..." -ForegroundColor Green

# Download win-acme (simple ACME client for Windows)
$winAcmeUrl = "https://github.com/win-acme/win-acme/releases/latest/download/win-acme.v2.2.9.1701.x64.pluggable.zip"
$downloadPath = "$env:TEMP\win-acme.zip"
$extractPath = "C:\win-acme"

try {
    Write-Host "📥 Downloading win-acme..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $winAcmeUrl -OutFile $downloadPath
    
    # Extract win-acme
    Write-Host "📦 Extracting win-acme..." -ForegroundColor Yellow
    if (Test-Path $extractPath) {
        Remove-Item $extractPath -Recurse -Force
    }
    Expand-Archive -Path $downloadPath -DestinationPath $extractPath
    
    Write-Host "✅ win-acme installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔐 To setup SSL certificate, run:" -ForegroundColor Yellow
    Write-Host "cd C:\win-acme" -ForegroundColor White
    Write-Host ".\wacs.exe --target manual --host lb-tech.co.uk --webroot C:\inetpub\wwwroot\empanalysis" -ForegroundColor White
    Write-Host ""
    Write-Host "📝 Important Notes:" -ForegroundColor Yellow
    Write-Host "1. Make sure your DNS is fully propagated first" -ForegroundColor White
    Write-Host "2. The domain lb-tech.co.uk must be accessible from the internet" -ForegroundColor White
    Write-Host "3. Port 80 must be open for ACME challenge" -ForegroundColor White
    Write-Host "4. The certificate will auto-renew every 60 days" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error downloading win-acme: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You can manually download from: https://github.com/win-acme/win-acme/releases" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 Manual SSL Setup Commands:" -ForegroundColor Green
Write-Host "1. cd C:\win-acme" -ForegroundColor White
Write-Host "2. .\wacs.exe --target manual --host lb-tech.co.uk" -ForegroundColor White 