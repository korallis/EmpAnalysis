# EmpAnalysis Setup Validation Script
# This script checks that everything is properly configured

Write-Host "🔍 EmpAnalysis Setup Validation" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Gray
Write-Host ""

$errors = 0

# Check project directories
Write-Host "📁 Checking project directories..." -ForegroundColor Yellow

$projects = @(
    "EmpAnalysis.Web",
    "EmpAnalysis.Agent", 
    "EmpAnalysis.Api",
    "EmpAnalysis.Shared"
)

foreach ($project in $projects) {
    if (Test-Path $project) {
        Write-Host "   ✅ $project directory exists" -ForegroundColor Green
        
        $csprojFile = Join-Path $project "$project.csproj"
        if (Test-Path $csprojFile) {
            Write-Host "   ✅ $project.csproj exists" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $project.csproj NOT FOUND" -ForegroundColor Red
            $errors++
        }
    } else {
        Write-Host "   ❌ $project directory NOT FOUND" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""

# Check startup scripts
Write-Host "📄 Checking startup scripts..." -ForegroundColor Yellow

$scripts = @(
    "start-web.ps1",
    "start-web.bat",
    "start-agent.ps1", 
    "start-agent.bat"
)

foreach ($script in $scripts) {
    if (Test-Path $script) {
        Write-Host "   ✅ $script exists" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $script NOT FOUND" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""

# Test .NET CLI
Write-Host "🔧 Testing .NET CLI..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "   ✅ .NET CLI available (version: $dotnetVersion)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ .NET CLI not available" -ForegroundColor Red
    $errors++
}

Write-Host ""

# Summary
Write-Host "📊 Validation Summary" -ForegroundColor Yellow
Write-Host "=====================" -ForegroundColor Gray

if ($errors -eq 0) {
    Write-Host "🎉 ALL CHECKS PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Setup is complete and ready to use" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Quick Start Commands:" -ForegroundColor Cyan
    Write-Host "   .\start-web.ps1     - Start web application" -ForegroundColor White
    Write-Host "   .\start-agent.ps1   - Start monitoring agent" -ForegroundColor White
    Write-Host ""
    Write-Host "🌐 Access URLs:" -ForegroundColor Cyan
    Write-Host "   http://lb-tech.co.uk:8080" -ForegroundColor White
    Write-Host "   https://lb-tech.co.uk:8443" -ForegroundColor White
} else {
    Write-Host "❌ VALIDATION FAILED" -ForegroundColor Red
    Write-Host "   Found $errors error(s)" -ForegroundColor Red
    Write-Host "   Please fix the issues above before proceeding" -ForegroundColor Yellow
}

Write-Host "" 