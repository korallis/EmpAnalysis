#!/bin/bash

echo "🚀 Starting EmpAnalysis System..."
echo "=================================="

# Function to check if port is in use
check_port() {
    netstat -an | grep ":$1 " > /dev/null 2>&1
    return $?
}

# Stop any existing dotnet processes
echo "Stopping existing processes..."
taskkill //F //IM dotnet.exe > /dev/null 2>&1 || echo "No existing processes found"

echo ""
echo "Starting API Server (Port 7001)..."
cd EmpAnalysis.Api
dotnet run --urls="https://localhost:7001" &
API_PID=$!
cd ..

echo "Waiting for API to start..."
sleep 8

echo ""
echo "Starting Web Application (Ports 8080/8443)..."
cd EmpAnalysis.Web  
dotnet run --urls="http://0.0.0.0:8080;https://0.0.0.0:8443" &
WEB_PID=$!
cd ..

echo "Waiting for Web to start..."
sleep 10

echo ""
echo "✅ System Started!"
echo "=================="
echo "🌐 Dashboard: https://localhost:8443"
echo "🔌 API: https://localhost:7001"
echo "📊 Swagger: https://localhost:7001/swagger"
echo "⚡ SignalR: https://localhost:7001/hubs/monitoring"
echo ""
echo "Press Ctrl+C to stop both services"

# Wait for user to stop
trap 'echo "Stopping services..."; kill $API_PID $WEB_PID 2>/dev/null; exit' INT
wait 