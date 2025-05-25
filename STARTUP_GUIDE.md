# 🚀 EmpAnalysis Startup Guide - ERROR-FREE VERSION

## ❌ CRITICAL: This Error Will Keep Happening If You Don't Follow This Guide

**"Couldn't find a project to run" - This happens when you run `dotnet run` from the wrong directory!**

### ❌ WRONG (ALWAYS causes error):
```powershell
PS C:\Projects\EmployeeMonitor> dotnet run --urls="http://0.0.0.0:8080;https://0.0.0.0:8443"
# ❌ ERROR: Couldn't find a project to run
```

## ✅ CORRECT METHODS (Fixed and Tested)

### 🌐 Start Web Application (CHOOSE ONE):

#### Option 1: PowerShell Script (RECOMMENDED)
```powershell
# MUST be in C:\Projects\EmployeeMonitor directory:
PS C:\Projects\EmployeeMonitor> .\start-web.ps1
```

#### Option 2: Batch File
```cmd
# MUST be in C:\Projects\EmployeeMonitor directory:
start-web.bat
```

#### Option 3: Manual (Navigate First)
```powershell
PS C:\Projects\EmployeeMonitor> cd EmpAnalysis.Web
PS C:\Projects\EmployeeMonitor\EmpAnalysis.Web> dotnet run --urls "http://0.0.0.0:8080;https://0.0.0.0:8443"
```

### 🕵️ Start Agent (CHOOSE ONE):

#### Option 1: PowerShell Script (RECOMMENDED)
```powershell
# MUST be in C:\Projects\EmployeeMonitor directory:
PS C:\Projects\EmployeeMonitor> .\start-agent.ps1
```

#### Option 2: Batch File
```cmd
# MUST be in C:\Projects\EmployeeMonitor directory:
start-agent.bat
```

#### Option 3: Manual (Navigate First)
```powershell
PS C:\Projects\EmployeeMonitor> cd EmpAnalysis.Agent
PS C:\Projects\EmployeeMonitor\EmpAnalysis.Agent> dotnet run --console
```

## 🔧 Recent Fixes Applied

### Fixed PowerShell Syntax Errors:
- ✅ **Fixed string terminator error** in `start-web.ps1`
- ✅ **Fixed URL quoting** - removed extra quotes around URLs
- ✅ **Fixed HTTPS port display** - now correctly shows 8443 instead of 8080
- ✅ **Tested and validated** - scripts now work without syntax errors

### Fixed Batch File Issues:
- ✅ **Updated URL quoting** for proper command line parsing
- ✅ **Corrected port displays** in output messages

## 📁 Required Directory Structure

```
C:\Projects\EmployeeMonitor/         ← YOU MUST BE HERE to run scripts!
├── start-web.ps1                    ← ✅ FIXED - Use this!
├── start-web.bat                    ← ✅ FIXED - Or this!
├── start-agent.ps1                  ← ✅ FIXED - Use this!
├── start-agent.bat                  ← ✅ FIXED - Or this!
├── test-setup.ps1                   ← ✅ NEW - Validates setup
├── EmpAnalysis.Web/                 ← Web project (has .csproj)
├── EmpAnalysis.Agent/               ← Agent project (has .csproj)
├── EmpAnalysis.Api/                 ← API project (has .csproj)
└── EmpAnalysis.Shared/              ← Shared project (has .csproj)
```

## ⚡ Quick Start (Error-Free Commands)

### Step 1: Validate Setup
```powershell
PS C:\Projects\EmployeeMonitor> .\test-setup.ps1
```

### Step 2: Start Web Application  
```powershell
PS C:\Projects\EmployeeMonitor> .\start-web.ps1
```

### Step 3: Start Agent (New Terminal)
```powershell
PS C:\Projects\EmployeeMonitor> .\start-agent.ps1
```

## 🌐 Access URLs (After Successful Start)

- **Primary HTTP**: http://lb-tech.co.uk:8080
- **Primary HTTPS**: https://lb-tech.co.uk:8443
- **Localhost HTTP**: http://localhost:8080
- **Localhost HTTPS**: https://localhost:8443

## 🎉 Success Indicators (What You Should See)

### Web Application Success:
```
🚀 Starting EmpAnalysis Web Application...
📁 Changed to directory: C:\Projects\EmployeeMonitor\EmpAnalysis.Web
✅ Project file found: EmpAnalysis.Web.csproj
🌐 Starting web application on:
   • HTTP:  http://lb-tech.co.uk:8080
   • HTTPS: https://lb-tech.co.uk:8443
Press Ctrl+C to stop the application
==========================================
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:8080
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://0.0.0.0:8443
```

### Agent Success:
```
🕵️ Starting EmpAnalysis Monitoring Agent...
📁 Changed to directory: C:\Projects\EmployeeMonitor\EmpAnalysis.Agent
✅ Project file found: EmpAnalysis.Agent.csproj
🔍 Starting monitoring agent in console mode...
EmpAnalysis Agent starting in console mode...
Press Ctrl+C to stop the agent.
info: EmpAnalysis.Agent.Services.MonitoringCoordinator[0]
      Monitoring Coordinator starting...
```

## 🚨 CRITICAL RULES TO PREVENT ERRORS

1. **NEVER** run `dotnet run` from `C:\Projects\EmployeeMonitor` 
2. **ALWAYS** use the startup scripts OR navigate to project directories first
3. **ALWAYS** check you're in the right directory before running commands
4. **ALWAYS** use the scripts from the root EmployeeMonitor directory

## 🔧 If You Still Get Errors

### Error: "Couldn't find a project to run"
**Cause**: Wrong directory  
**Fix**: Use the startup scripts or navigate to project directory first

### Error: "The string is missing the terminator"
**Cause**: Old version of scripts  
**Fix**: Scripts are now fixed - make sure you're using the updated versions

### Error: "Permission denied"
**Cause**: Need admin rights for agent monitoring  
**Fix**: Run PowerShell as Administrator

### Still having issues?
Run the validation script to check your setup:
```powershell
PS C:\Projects\EmployeeMonitor> .\test-setup.ps1
```

## 📁 Project Structure

```
EmployeeMonitor/                    ← Root directory (NO .csproj here!)
├── start-web.ps1                   ← Use this to start web app
├── start-agent.ps1                 ← Use this to start agent
├── EmpAnalysis.Web/                ← Web project directory
│   └── EmpAnalysis.Web.csproj      ← Project file is HERE
├── EmpAnalysis.Agent/              ← Agent project directory
│   └── EmpAnalysis.Agent.csproj    ← Project file is HERE
├── EmpAnalysis.Api/                ← API project directory
│   └── EmpAnalysis.Api.csproj      ← Project file is HERE
└── EmpAnalysis.Shared/             ← Shared project directory
    └── EmpAnalysis.Shared.csproj   ← Project file is HERE
```

## 🎯 Quick Start Commands

### Option A: Use Scripts (Easiest)
```powershell
# Start web application:
.\start-web.ps1

# Start monitoring agent (in another terminal):
.\start-agent.ps1
```

### Option B: Manual Commands
```powershell
# Terminal 1 - Web Application:
cd EmpAnalysis.Web
dotnet run --urls="http://0.0.0.0:8080;https://0.0.0.0:8443"

# Terminal 2 - Monitoring Agent:
cd EmpAnalysis.Agent
dotnet run --console
```

## 🌐 Access URLs

- **HTTP**: http://lb-tech.co.uk:8080
- **HTTPS**: https://lb-tech.co.uk:8443
- **Local HTTP**: http://localhost:8080
- **Local HTTPS**: https://localhost:8443

## 🔧 Troubleshooting

### Problem: "Couldn't find a project to run"
**Solution**: Make sure you're in a project directory (with a .csproj file) or use the startup scripts.

### Problem: "Permission denied" or monitoring features not working
**Solution**: Run PowerShell as Administrator for the monitoring agent.

### Problem: "Port already in use"
**Solution**: Stop any existing dotnet processes:
```powershell
Get-Process dotnet | Stop-Process -Force
```

### Problem: SSL certificate errors
**Solution**: Accept the development certificate:
```powershell
dotnet dev-certs https --trust
```

## 📋 Development Workflow

1. **Always start from the root directory**: `C:\Projects\EmployeeMonitor`
2. **Use the startup scripts** for convenience
3. **Or navigate to specific project directories** before running dotnet commands
4. **Never run `dotnet run` from the root directory** - it will fail!

## 🎉 Success Indicators

### Web Application Started Successfully:
```
✅ Project file found: EmpAnalysis.Web.csproj
🌐 Starting web application on:
   • HTTP:  http://lb-tech.co.uk:8080
   • HTTPS: https://lb-tech.co.uk:8080

Now listening on: http://0.0.0.0:8080
Now listening on: https://0.0.0.0:8443
Application started. Press Ctrl+C to shut down.
```

### Agent Started Successfully:
```
✅ Project file found: EmpAnalysis.Agent.csproj
🔍 Starting monitoring agent in console mode...

EmpAnalysis Agent starting in console mode...
Press Ctrl+C to stop the agent.
info: EmpAnalysis.Agent.Services.MonitoringCoordinator[0]
      Monitoring Coordinator starting...
```

## 🚨 Remember

- **ALWAYS** be in the correct project directory when using `dotnet run`
- **OR** use the provided startup scripts from the root directory
- **NEVER** run `dotnet run` from `C:\Projects\EmployeeMonitor` directly 