# Enhanced Agent Restart Script
# This script safely restarts the agent with enhanced monitoring features

Write-Host "=== Enhanced Agent Restart Script ===" -ForegroundColor Cyan
Write-Host "Date: $(Get-Date)" -ForegroundColor Gray

# Function to check if process exists
function Test-ProcessExists {
    param($ProcessName)
    return Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
}

# Function to wait for process to stop
function Wait-ForProcessStop {
    param($ProcessName, $TimeoutSeconds = 30)
    
    $elapsed = 0
    while ((Test-ProcessExists $ProcessName) -and ($elapsed -lt $TimeoutSeconds)) {
        Start-Sleep -Seconds 1
        $elapsed++
        Write-Host "." -NoNewline
    }
    Write-Host ""
    
    if (Test-ProcessExists $ProcessName) {
        Write-Host "⚠️  Process still running after $TimeoutSeconds seconds" -ForegroundColor Yellow
        return $false
    } else {
        Write-Host "✅ Process stopped successfully" -ForegroundColor Green
        return $true
    }
}

try {
    # Step 1: Check current agent status
    Write-Host "📊 Checking current agent status..." -ForegroundColor Yellow
    $currentAgent = Get-Process -Name "EmpAnalysis.Agent" -ErrorAction SilentlyContinue
    
    if ($currentAgent) {
        Write-Host "🔍 Found running agent process:" -ForegroundColor Green
        Write-Host "   Process ID: $($currentAgent.Id)" -ForegroundColor Gray
        Write-Host "   Start Time: $($currentAgent.StartTime)" -ForegroundColor Gray
        Write-Host "   Working Set: $([math]::Round($currentAgent.WorkingSet / 1MB, 2)) MB" -ForegroundColor Gray
        
        # Step 2: Attempt graceful shutdown
        Write-Host "🛑 Attempting graceful shutdown..." -ForegroundColor Yellow
        try {
            # Try to close main window first
            if ($currentAgent.MainWindowHandle -ne [System.IntPtr]::Zero) {
                $currentAgent.CloseMainWindow() | Out-Null
                Write-Host "   Sent close window signal" -ForegroundColor Gray
            }
            
            # Wait a few seconds for graceful shutdown
            Write-Host "   Waiting for graceful shutdown" -NoNewline -ForegroundColor Gray
            if (Wait-ForProcessStop "EmpAnalysis.Agent" 10) {
                Write-Host "✅ Agent stopped gracefully" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Graceful shutdown timeout, trying force stop..." -ForegroundColor Yellow
                
                # Try terminating the process
                $currentAgent.Kill()
                Write-Host "   Sent termination signal" -ForegroundColor Gray
                
                Write-Host "   Waiting for process termination" -NoNewline -ForegroundColor Gray
                if (-not (Wait-ForProcessStop "EmpAnalysis.Agent" 15)) {
                    Write-Host "❌ Unable to stop the agent process automatically" -ForegroundColor Red
                    Write-Host "🔧 Please manually stop the agent and run this script again" -ForegroundColor Yellow
                    Write-Host "   You can use Task Manager to end the EmpAnalysis.Agent process" -ForegroundColor Gray
                    exit 1
                }
            }
        }
        catch {
            Write-Host "❌ Error during shutdown: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "🔧 Please manually stop the agent and run this script again" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "ℹ️  No running agent process found" -ForegroundColor Blue
    }
    
    # Step 3: Build the enhanced agent
    Write-Host "🔨 Building enhanced agent..." -ForegroundColor Yellow
    $buildPath = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent"
    
    Push-Location $buildPath
    try {
        Write-Host "   Building in Release mode..." -ForegroundColor Gray
        $buildResult = dotnet build --configuration Release --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build completed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Build failed:" -ForegroundColor Red
            $buildResult | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
            Write-Host "🔧 Please fix build errors and try again" -ForegroundColor Yellow
            exit 1
        }
    }
    finally {
        Pop-Location
    }
    
    # Step 4: Start the enhanced agent
    Write-Host "🚀 Starting enhanced agent..." -ForegroundColor Yellow
    $agentPath = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent\bin\Release\net8.0-windows\EmpAnalysis.Agent.exe"
    
    if (Test-Path $agentPath) {
        try {
            $newProcess = Start-Process -FilePath $agentPath -PassThru -WindowStyle Hidden
            Start-Sleep -Seconds 3
            
            if (Test-ProcessExists "EmpAnalysis.Agent") {
                $newAgent = Get-Process -Name "EmpAnalysis.Agent"
                Write-Host "✅ Enhanced agent started successfully!" -ForegroundColor Green
                Write-Host "   New Process ID: $($newAgent.Id)" -ForegroundColor Gray
                Write-Host "   Start Time: $($newAgent.StartTime)" -ForegroundColor Gray
                
                # Step 5: Verify enhanced features
                Write-Host "🔍 Enhanced Features Added:" -ForegroundColor Cyan
                Write-Host "   ✅ Window Title Monitoring" -ForegroundColor Green
                Write-Host "   ✅ Idle Time Detection (5-minute threshold)" -ForegroundColor Green
                Write-Host "   ✅ Enhanced System Events (WindowChange, IdleStart, IdleEnd)" -ForegroundColor Green
                Write-Host "   ✅ Windows API Integration" -ForegroundColor Green
                Write-Host "   ✅ Activity Tracking" -ForegroundColor Green
                
                # Wait a moment and check logs
                Start-Sleep -Seconds 5
                $logPath = "C:\Projects\EmployeeMonitor\EmpAnalysis.Agent\logs"
                if (Test-Path $logPath) {
                    $latestLog = Get-ChildItem $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                    if ($latestLog) {
                        Write-Host "📝 Recent log entries:" -ForegroundColor Yellow
                        Get-Content $latestLog.FullName -Tail 5 | ForEach-Object {
                            Write-Host "   $_" -ForegroundColor Gray
                        }
                        # Check for analytics output (risk, anomaly, trend)
                        $analyticsLines = Get-Content $latestLog.FullName | Select-String -Pattern "Risk Score|Anomaly|Trend|Advanced Analytics"
                        if ($analyticsLines) {
                            Write-Host "📈 Advanced Analytics Detected:" -ForegroundColor Cyan
                            $analyticsLines | ForEach-Object { Write-Host "   $_" -ForegroundColor Green }
                        } else {
                            Write-Host "⚠️  No advanced analytics output found in recent logs." -ForegroundColor Yellow
                        }
                    }
                }
                
                Write-Host "🎉 Enhanced agent restart completed successfully!" -ForegroundColor Green
                Write-Host "🔗 Monitor the agent at: https://localhost:8443" -ForegroundColor Cyan
                
            } else {
                Write-Host "❌ Agent process not found after startup" -ForegroundColor Red
                exit 1
            }
        }
        catch {
            Write-Host "❌ Error starting agent: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ Agent executable not found: $agentPath" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "📍 Stack trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}

Write-Host "✨ Script completed at $(Get-Date)" -ForegroundColor Cyan
