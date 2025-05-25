@echo off
echo INITIALIZING GIT REPOSITORY FOR EMPANALYSIS
echo ==========================================

echo.
echo Checking Git installation...
git --version
if errorlevel 1 (
    echo ERROR: Git is not installed or not in PATH
    pause
    exit /b 1
)

echo.
echo Checking if Git repository exists...
if exist .git (
    echo Git repository already exists
    goto status
)

echo.
echo Initializing new Git repository...
git init

echo.
echo Configuring Git user...
git config user.name "EmpAnalysis Developer"
git config user.email "developer@empanalysis.com"

echo.
echo Git repository initialized successfully!

:status
echo.
echo REPOSITORY STATUS:
echo ==================
git status --short

echo.
echo FILES TO COMMIT:
for /f %%i in ('git status --porcelain ^| find /c /v ""') do echo Total files: %%i

echo.
echo NEXT STEPS:
echo ===========
echo 1. Review files: git status
echo 2. Add all files: git add .
echo 3. Create commit: git commit -m "Initial commit: EmpAnalysis Phase 3"
echo 4. Create GitHub repository 'empanalysis'
echo 5. Add remote: git remote add origin https://github.com/username/empanalysis.git
echo 6. Push to GitHub: git push -u origin main

echo.
echo READY FOR GITHUB PUBLICATION!
pause 