@echo off
echo 🕵️ Starting EmpAnalysis Monitoring Agent...
echo.

REM Check if we're in the correct location
if not exist "EmpAnalysis.Agent" (
    echo ❌ Error: EmpAnalysis.Agent directory not found!
    echo    Make sure this script is in the root EmployeeMonitor directory.
    pause
    exit /b 1
)

REM Change to the agent project directory
cd EmpAnalysis.Agent
echo 📁 Changed to directory: %CD%

REM Check if the project file exists
if not exist "EmpAnalysis.Agent.csproj" (
    echo ❌ Error: EmpAnalysis.Agent.csproj not found in current directory!
    pause
    exit /b 1
)

echo ✅ Project file found: EmpAnalysis.Agent.csproj
echo.

echo 🔍 Starting monitoring agent in console mode...
echo    • Screenshots: Every 5 minutes during work hours
echo    • Activity: Every 30 seconds
echo    • Data sync: Every 5 minutes
echo.
echo Press Ctrl+C to stop the agent
echo ==========================================

REM Run the agent in console mode
dotnet run --console 