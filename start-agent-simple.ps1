# Phase 4: Simple Agent Starter
# Start the monitoring agent for testing

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Starting EmpAnalysis Monitoring Agent" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Build the agent first
Write-Host "[1/2] Building Agent..." -ForegroundColor Yellow
$buildResult = dotnet build "EmpAnalysis.Agent\EmpAnalysis.Agent.csproj" --configuration Debug --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Agent built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Agent build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Start the agent
Write-Host "[2/2] Starting Agent..." -ForegroundColor Yellow
Write-Host "Agent will connect to API at: https://localhost:7001" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the agent" -ForegroundColor Yellow
Write-Host ""

# Set employee ID from current user
$env:EMPLOYEE_ID = $env:USERNAME

# Run the agent
dotnet run --project "EmpAnalysis.Agent\EmpAnalysis.Agent.csproj" --configuration Debug
