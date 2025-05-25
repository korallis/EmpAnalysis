@echo off
echo Starting EmpAnalysis Dashboard...
echo.

REM Navigate to the project root directory if needed
if not exist "EmpAnalysis.Web" (
    echo Looking for EmpAnalysis.Web directory...
    if exist "C:\Projects\EmployeeMonitor\EmpAnalysis.Web" (
        cd /d "C:\Projects\EmployeeMonitor"
        echo Found project at: C:\Projects\EmployeeMonitor
    ) else (
        echo Error: Could not find EmpAnalysis.Web directory
        echo Please ensure you're in the correct directory
        pause
        exit /b 1
    )
)

echo Building and starting the application...
echo.

REM Use --project parameter to ensure it works from any directory
dotnet run --project EmpAnalysis.Web --urls="http://0.0.0.0:8080;https://0.0.0.0:8443"

echo.
echo Application has stopped.
pause 