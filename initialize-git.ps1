# Initialize Git Repository for EmpAnalysis
# Phase 3 - GitHub Publication

Write-Host "INITIALIZING GIT REPOSITORY..." -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Cyan

# Check if Git is installed
try {
    $gitVersion = git --version
    Write-Host "Git Version: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Git is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Initialize repository if not already initialized
if (-not (Test-Path ".git")) {
    Write-Host "Initializing new Git repository..." -ForegroundColor Blue
    git init
    
    Write-Host "Configuring Git user..." -ForegroundColor Blue
    git config user.name "EmpAnalysis Developer"
    git config user.email "developer@empanalysis.com"
    
    Write-Host "Git repository initialized successfully!" -ForegroundColor Green
} else {
    Write-Host "Git repository already exists" -ForegroundColor Yellow
}

# Check repository status
Write-Host "" 
Write-Host "REPOSITORY STATUS:" -ForegroundColor Blue
git status --short

Write-Host ""
Write-Host "FILES TO COMMIT:" -ForegroundColor Blue
$fileCount = (git status --porcelain | Measure-Object).Count
Write-Host "Total files: $fileCount" -ForegroundColor White

Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Review files to commit: git status" -ForegroundColor White
Write-Host "2. Add all files: git add ." -ForegroundColor White
Write-Host "3. Create initial commit: git commit -m 'Initial commit'" -ForegroundColor White
Write-Host "4. Create GitHub repository 'empanalysis'" -ForegroundColor White
Write-Host "5. Add remote: git remote add origin https://github.com/username/empanalysis.git" -ForegroundColor White
Write-Host "6. Push to GitHub: git push -u origin main" -ForegroundColor White

Write-Host ""
Write-Host "READY FOR GITHUB PUBLICATION!" -ForegroundColor Green 