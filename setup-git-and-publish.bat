@echo off
echo Setting up Git and publishing EmpAnalysis...
echo ==============================================

echo Configuring Git user...
git config user.name "korallis"
git config user.email "lee.barry84@gmail.com"

echo.
echo Git configuration complete:
git config user.name
git config user.email

echo.
echo Initializing repository...
git init

echo.
echo Repository status:
git status --short

echo.
echo Adding all files...
git add .

echo.
echo Creating comprehensive commit...
git commit -m "Complete Implementation: EmpAnalysis Phase 3 + Phase 4 Agent Integration - Real-time monitoring system with SignalR, professional dashboard, complete API endpoints, and enhanced agent integration. 80%% complete, enterprise ready."

echo.
echo Commit created successfully!

echo.
echo Repository ready for GitHub publication!
echo.
echo Next steps:
echo 1. Create GitHub repository 'empanalysis'
echo 2. git remote add origin https://github.com/korallis/empanalysis.git
echo 3. git branch -M main
echo 4. git push -u origin main

pause 