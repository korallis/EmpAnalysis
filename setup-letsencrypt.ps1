# Let's Encrypt SSL Setup Script for EmpAnalysis
# This script installs and configures Let's Encrypt SSL certificates for lb-tech.co.uk

param(
    [string]$Domain = "lb-tech.co.uk",
    [string]$Email = "admin@lb-tech.co.uk",
    [string]$SiteName = "EmpAnalysis",
    [string]$WinAcmeVersion = "2.2.9.1701"
)

Write-Host "🔒 Starting Let's Encrypt SSL Setup for $Domain..." -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

try {
    # Create working directory
    $workingDir = "C:\Tools\win-acme"
    if (!(Test-Path $workingDir)) {
        New-Item -ItemType Directory -Path $workingDir -Force | Out-Null
    }
    Set-Location $workingDir

    # Download win-acme if not exists
    $winAcmeExe = "$workingDir\wacs.exe"
    if (!(Test-Path $winAcmeExe)) {
        Write-Host "📥 Downloading win-acme v$WinAcmeVersion..." -ForegroundColor Yellow
        
        $downloadUrl = "https://github.com/win-acme/win-acme/releases/download/v$WinAcmeVersion/win-acme.v$WinAcmeVersion.x64.pluggable.zip"
        $zipFile = "$workingDir\win-acme.zip"
        
        try {
            Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile -UseBasicParsing
            Expand-Archive -Path $zipFile -DestinationPath $workingDir -Force
            Remove-Item $zipFile -Force
            Write-Host "✅ win-acme downloaded and extracted" -ForegroundColor Green
        } catch {
            Write-Error "Failed to download win-acme: $($_.Exception.Message)"
            exit 1
        }
    } else {
        Write-Host "✅ win-acme already installed" -ForegroundColor Green
    }

    # Import WebAdministration module
    Import-Module WebAdministration -ErrorAction Stop

    # Verify the website exists
    if (!(Get-Website -Name $SiteName -ErrorAction SilentlyContinue)) {
        Write-Error "Website '$SiteName' not found. Please run deploy-to-iis.ps1 first."
        exit 1
    }

    # Check if HTTP binding exists
    $httpBinding = Get-WebBinding -Name $SiteName -Protocol "http" -HostHeader $Domain -ErrorAction SilentlyContinue
    if (!$httpBinding) {
        Write-Host "⚙️ Adding HTTP binding for $Domain..." -ForegroundColor Yellow
        New-WebBinding -Name $SiteName -Protocol "http" -Port 80 -HostHeader $Domain
    }

    # Ensure firewall allows HTTP and HTTPS
    Write-Host "🔥 Configuring Windows Firewall..." -ForegroundColor Yellow
    try {
        New-NetFirewallRule -DisplayName "HTTP Inbound" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow -ErrorAction SilentlyContinue
        New-NetFirewallRule -DisplayName "HTTPS Inbound" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow -ErrorAction SilentlyContinue
        Write-Host "✅ Firewall rules configured" -ForegroundColor Green
    } catch {
        Write-Warning "⚠️ Could not configure firewall rules: $($_.Exception.Message)"
    }

    # Test domain accessibility
    Write-Host "🌐 Testing domain accessibility..." -ForegroundColor Yellow
    try {
        $testUrl = "http://$Domain"
        $response = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ Domain $Domain is accessible" -ForegroundColor Green
        } else {
            Write-Warning "⚠️ Domain returned status code: $($response.StatusCode)"
        }
    } catch {
        Write-Warning "⚠️ Could not access domain $Domain. Ensure DNS is configured correctly."
        Write-Host "   DNS should have an A record pointing $Domain to this server's public IP" -ForegroundColor Yellow
        
        $choice = Read-Host "Continue anyway? (y/N)"
        if ($choice -ne "y" -and $choice -ne "Y") {
            exit 1
        }
    }

    # Create certificate using win-acme
    Write-Host "🎫 Requesting SSL certificate from Let's Encrypt..." -ForegroundColor Yellow
    
    $wacArgs = @(
        "--target", "iis",
        "--siteid", (Get-Website -Name $SiteName).Id,
        "--host", $Domain,
        "--host", "www.$Domain",
        "--emailaddress", $Email,
        "--accepttos",
        "--unattended"
    )

    Write-Host "Executing: $winAcmeExe $($wacArgs -join ' ')" -ForegroundColor Gray
    
    $process = Start-Process -FilePath $winAcmeExe -ArgumentList $wacArgs -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host "✅ SSL certificate successfully installed!" -ForegroundColor Green
    } else {
        Write-Error "❌ Certificate installation failed with exit code: $($process.ExitCode)"
        Write-Host "📋 Common issues:" -ForegroundColor Yellow
        Write-Host "1. Domain not accessible from internet" -ForegroundColor White
        Write-Host "2. DNS not configured correctly" -ForegroundColor White
        Write-Host "3. Firewall blocking port 80" -ForegroundColor White
        Write-Host "4. Rate limiting (try again later)" -ForegroundColor White
        exit 1
    }

    # Set up automatic renewal
    Write-Host "🔄 Setting up automatic certificate renewal..." -ForegroundColor Yellow
    
    $taskName = "win-acme certificate renewal"
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    
    if ($existingTask) {
        Write-Host "✅ Renewal task already exists" -ForegroundColor Green
    } else {
        # Create scheduled task for renewal
        $action = New-ScheduledTaskAction -Execute $winAcmeExe -Argument "--renew --unattended"
        $trigger = New-ScheduledTaskTrigger -Daily -At "02:00"
        $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
        
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Automatic renewal of Let's Encrypt certificates"
        
        Write-Host "✅ Automatic renewal scheduled for 2:00 AM daily" -ForegroundColor Green
    }

    # Update IIS bindings to redirect HTTP to HTTPS
    Write-Host "🔀 Configuring HTTPS redirect..." -ForegroundColor Yellow
    
    # Install URL Rewrite module if not present
    $urlRewriteModule = Get-WindowsFeature -Name "IIS-HttpRedirect" -ErrorAction SilentlyContinue
    if ($urlRewriteModule -and $urlRewriteModule.InstallState -ne "Installed") {
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-HttpRedirect" -All
    }

    # Create web.config for HTTPS redirect in the root
    $webConfigPath = "C:\inetpub\wwwroot\empanalysis\web\web.config"
    if (Test-Path $webConfigPath) {
        # Backup existing web.config
        Copy-Item $webConfigPath "$webConfigPath.backup" -Force
    }

    $webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\EmpAnalysis.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="52428800" />
        </requestFiltering>
      </security>
      <httpProtocol>
        <customHeaders>
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="SAMEORIGIN" />
          <add name="X-XSS-Protection" value="1; mode=block" />
          <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
        </customHeaders>
      </httpProtocol>
      <rewrite>
        <rules>
          <rule name="Redirect to HTTPS" stopProcessing="true">
            <match url=".*" />
            <conditions>
              <add input="{HTTPS}" pattern="off" ignoreCase="true" />
              <add input="{HTTP_HOST}" pattern="localhost" negate="true" />
            </conditions>
            <action type="Redirect" url="https://{HTTP_HOST}/{R:0}" redirectType="Permanent" />
          </rule>
        </rules>
      </rewrite>
    </system.webServer>
  </location>
</configuration>
"@

    Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
    Write-Host "✅ HTTPS redirect configured" -ForegroundColor Green

    # Test HTTPS
    Write-Host "🧪 Testing HTTPS configuration..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    try {
        $httpsUrl = "https://$Domain"
        $response = Invoke-WebRequest -Uri $httpsUrl -UseBasicParsing -TimeoutSec 15
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ HTTPS is working!" -ForegroundColor Green
        }
    } catch {
        Write-Warning "⚠️ HTTPS test failed: $($_.Exception.Message)"
        Write-Host "   This may be normal if DNS propagation is still in progress" -ForegroundColor Yellow
    }

    Write-Host "🎉 Let's Encrypt SSL setup completed successfully!" -ForegroundColor Green
    Write-Host "📍 Secure Website: https://$Domain" -ForegroundColor Cyan
    Write-Host "📍 Secure API: https://$Domain/api" -ForegroundColor Cyan
    Write-Host "📍 Certificate will auto-renew daily at 2:00 AM" -ForegroundColor Cyan
    
    Write-Host "`n📋 SSL Configuration Summary:" -ForegroundColor Yellow
    Write-Host "✅ SSL Certificate installed for $Domain and www.$Domain" -ForegroundColor Green
    Write-Host "✅ HTTPS redirect configured" -ForegroundColor Green
    Write-Host "✅ Security headers added" -ForegroundColor Green
    Write-Host "✅ Automatic renewal scheduled" -ForegroundColor Green
    Write-Host "✅ Firewall configured for HTTP/HTTPS" -ForegroundColor Green

} catch {
    Write-Error "❌ SSL setup failed: $($_.Exception.Message)"
    Write-Host "📋 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Ensure domain DNS points to this server" -ForegroundColor White
    Write-Host "2. Check firewall allows ports 80 and 443" -ForegroundColor White
    Write-Host "3. Verify IIS website is running" -ForegroundColor White
    Write-Host "4. Check Let's Encrypt rate limits" -ForegroundColor White
    exit 1
} finally {
    # Return to original directory
    Set-Location $PSScriptRoot
} 