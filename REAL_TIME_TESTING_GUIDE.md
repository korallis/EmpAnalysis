# Real-time Employee Monitoring System - Testing Guide

## 🔍 Current Status (✅ RUNNING)

**Services Status:**
- ✅ **API Server**: Running on `https://localhost:7001` (Process ID: 15936)
- ✅ **Web Dashboard**: Running on `http://localhost:8080` and `https://localhost:8443` (Process ID: 26552)
- ✅ **SignalR Hub**: Available at `https://localhost:7001/hubs/monitoring`
- ✅ **Database**: Connected and running migrations

## 🎯 Phase 3 Features Implemented

### ✅ SignalR Real-time System
- **MonitoringHub.cs**: Real-time communication hub with group management
- **Live Event Broadcasting**: Automatic notifications on data submission
- **Auto-reconnection**: Client automatically reconnects if connection is lost
- **Connection Status**: Visual indicator showing "Live Data Connected"

### ✅ Enhanced Dashboard
- **Real-time Metrics**: Live updates without page refresh
- **Interactive Charts**: Chart.js integration with live data updates
- **Trend Animations**: Visual indicators for metric changes
- **Activity Feed**: Real-time activity log with type-specific icons

### ✅ API Endpoints
- **POST /api/monitoring/session** - Submit monitoring session
- **POST /api/monitoring/screenshot** - Submit screenshot data
- **POST /api/monitoring/activity** - Submit activity tracking
- **GET /api/monitoring/dashboard** - Get live dashboard metrics

## 🧪 Testing Instructions

### 1. Open Dashboard
```
Browser URLs:
• HTTP:  http://localhost:8080
• HTTPS: https://localhost:8443
```

**What to look for:**
- ✅ "Live Data Connected" indicator (top-right, green)
- ✅ Real-time metrics updating every 5 seconds
- ✅ Interactive charts with live data
- ✅ Activity feed showing recent events

### 2. Test Real-time Data Submission

#### Option A: Using PowerShell (Windows)
```powershell
# Test Session Data
$sessionData = @{
    EmployeeId = "test-employee-123"
    SessionStart = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    WorkstationInfo = "Test Workstation"
    IPAddress = "192.168.1.100"
    MacAddress = "00:11:22:33:44:55"
    OperatingSystem = "Windows 11"
    SessionType = "Regular"
    Department = "IT"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7001/api/monitoring/session" `
    -Method POST `
    -Body $sessionData `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

#### Option B: Using Postman
```
POST https://localhost:7001/api/monitoring/session
Content-Type: application/json

{
  "employeeId": "test-employee-123",
  "sessionStart": "2025-05-24T23:00:00Z",
  "workstationInfo": "Test Workstation",
  "ipAddress": "192.168.1.100",
  "macAddress": "00:11:22:33:44:55",
  "operatingSystem": "Windows 11",
  "sessionType": "Regular",
  "department": "IT"
}
```

#### Option C: Using curl
```bash
curl -k -X POST https://localhost:7001/api/monitoring/session \
  -H "Content-Type: application/json" \
  -d '{
    "employeeId": "test-employee-123",
    "sessionStart": "2025-05-24T23:00:00Z",
    "workstationInfo": "Test Workstation",
    "ipAddress": "192.168.1.100",
    "macAddress": "00:11:22:33:44:55",
    "operatingSystem": "Windows 11",
    "sessionType": "Regular",
    "department": "IT"
  }'
```

### 3. Expected Real-time Behavior

**After submitting data, you should see:**
1. **Immediate Dashboard Update**: Metrics refresh within seconds
2. **Activity Feed Update**: New entry appears in activity log
3. **Chart Animation**: Charts update with new data points
4. **Trend Indicators**: Arrows show increase/decrease trends
5. **No Page Refresh**: All updates happen automatically

### 4. Test Different Data Types

#### Screenshot Data
```json
POST /api/monitoring/screenshot
{
  "employeeId": "test-employee-123",
  "imageData": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==",
  "capturedAt": "2025-05-24T23:05:00Z",
  "windowTitle": "Microsoft Word - Document1",
  "applicationName": "Microsoft Word",
  "isBlurred": false
}
```

#### Activity Data
```json
POST /api/monitoring/activity
{
  "employeeId": "test-employee-123",
  "activityType": "ApplicationUsage",
  "description": "Used Microsoft Teams for 30 minutes",
  "timestamp": "2025-05-24T23:10:00Z",
  "applicationName": "Microsoft Teams",
  "duration": 1800,
  "isProductiveActivity": true
}
```

## 🔧 Troubleshooting

### Connection Issues
- **Red "Disconnected" indicator**: Check if API is running on port 7001
- **Charts not updating**: Verify SignalR connection in browser console
- **CORS errors**: API should allow origins from ports 8080 and 8443

### Service Management
```powershell
# Check running processes
Get-Process -Name "*EmpAnalysis*"

# Check port availability
Test-NetConnection -ComputerName localhost -Port 7001
Test-NetConnection -ComputerName localhost -Port 8080

# View recent logs
Get-Content "EmpAnalysis.Api\logs\empanalysis-$(Get-Date -Format 'yyyyMMdd').txt" -Tail 20
```

### Restart Services
```powershell
# Stop all services
Get-Process -Name "*EmpAnalysis*" | Stop-Process -Force

# Start API
cd EmpAnalysis.Api
dotnet run --urls "https://localhost:7001"

# Start Web (in another terminal)
cd EmpAnalysis.Web
dotnet run --urls "http://localhost:8080;https://localhost:8443"
```

## 📊 Performance Monitoring

**Real-time Metrics Tracked:**
- Active Employees Count
- Daily Productive Hours
- Screenshot Count
- Risk Alerts
- Productivity Score
- Network Events

**Update Frequency:**
- Dashboard metrics: Every 5 seconds
- SignalR events: Immediate
- Chart animations: Smooth transitions
- Connection status: Real-time

## 🚀 Next Steps

1. **Agent Integration**: Connect existing EmpAnalysis.Agent to new endpoints
2. **Advanced Analytics**: Implement productivity scoring algorithms
3. **Alert System**: Set up automated notifications for risk events
4. **User Management**: Add role-based access control
5. **Reporting**: Generate scheduled reports

## 📝 Testing Log

Record your testing results:
- [ ] Dashboard loads with "Live Data Connected"
- [ ] Metrics update every 5 seconds
- [ ] Charts are interactive and responsive
- [ ] Activity feed shows real-time entries
- [ ] Data submission triggers immediate updates
- [ ] Connection status reflects actual state
- [ ] Trend animations work properly
- [ ] All endpoints respond correctly

---

**Last Updated**: May 24, 2025
**System Status**: ✅ OPERATIONAL
**Testing Status**: ✅ READY FOR PHASE 4 