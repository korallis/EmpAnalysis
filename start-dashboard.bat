@echo off
:: Universal Dashboard Startup Batch Script
:: This script can be run from the root directory without navigation issues

setlocal enabledelayedexpansion

:: Default values
set "PROJECT=web"
set "URLS=http://*:8080;https://*:8443"

:: Parse command line arguments
if "%1"=="web" set "PROJECT=web"
if "%1"=="api" (
    set "PROJECT=api"
    set "URLS=http://*:7001;https://*:7000"
)
if "%1"=="agent" set "PROJECT=agent"
if "%1"=="help" goto :help
if "%1"=="--help" goto :help
if "%1"=="/?" goto :help

:: Set project paths
if "%PROJECT%"=="web" (
    set "PROJECT_PATH=EmpAnalysis.Web"
    set "PROJECT_NAME=Employee Monitoring Dashboard"
)
if "%PROJECT%"=="api" (
    set "PROJECT_PATH=EmpAnalysis.Api"
    set "PROJECT_NAME=Employee Monitoring API"
)
if "%PROJECT%"=="agent" (
    set "PROJECT_PATH=EmpAnalysis.Agent"
    set "PROJECT_NAME=Employee Monitoring Agent"
)

:: Check if project directory exists
if not exist "%PROJECT_PATH%" (
    echo ❌ Project directory not found: %PROJECT_PATH%
    echo Available projects: web, api, agent
    pause
    exit /b 1
)

:: Display startup information
echo.
echo 🚀 Starting %PROJECT_NAME%...
echo 📁 Project: %PROJECT_PATH%
echo 🔗 URLs: %URLS%
echo.
echo Press Ctrl+C to stop the application
echo ════════════════════════════════════════════════════
echo.

:: Change to project directory and run
cd /d "%PROJECT_PATH%"

if "%PROJECT%"=="agent" (
    dotnet run
) else (
    dotnet run --urls "%URLS%"
)

:: Return to original directory
cd /d "%~dp0"

echo.
echo 🛑 Application stopped.
pause
exit /b 0

:help
echo.
echo Universal Dashboard Startup Script
echo.
echo Usage:
echo   start-dashboard.bat           # Start web dashboard
echo   start-dashboard.bat web       # Start web dashboard
echo   start-dashboard.bat api       # Start API
echo   start-dashboard.bat agent     # Start agent
echo   start-dashboard.bat help      # Show this help
echo.
echo Projects:
echo   web    - Employee Monitoring Dashboard (Port 8080/8443)
echo   api    - Employee Monitoring API (Port 7001/7000)
echo   agent  - Employee Monitoring Agent (Windows Service)
echo.
pause
exit /b 0 