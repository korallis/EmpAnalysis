@echo off
echo 🚀 Starting EmpAnalysis Web Application...
echo.

REM Check if we're in the correct location
if not exist "EmpAnalysis.Web" (
    echo ❌ Error: EmpAnalysis.Web directory not found!
    echo    Make sure this script is in the root EmployeeMonitor directory.
    pause
    exit /b 1
)

REM Change to the web project directory
cd EmpAnalysis.Web
echo 📁 Changed to directory: %CD%

REM Check if the project file exists
if not exist "EmpAnalysis.Web.csproj" (
    echo ❌ Error: EmpAnalysis.Web.csproj not found in current directory!
    pause
    exit /b 1
)

echo ✅ Project file found: EmpAnalysis.Web.csproj
echo.

echo 🌐 Starting web application on:
echo    • HTTP:  http://lb-tech.co.uk:8080
echo    • HTTPS: https://lb-tech.co.uk:8443
echo.
echo Press Ctrl+C to stop the application
echo ==========================================

REM Run the application with proper quoting
dotnet run --urls "http://0.0.0.0:8080;https://0.0.0.0:8443" 