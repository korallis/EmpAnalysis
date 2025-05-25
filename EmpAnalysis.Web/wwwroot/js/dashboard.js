// Dashboard Charts and Interactivity

// Initialize Productivity Chart
window.initializeProductivityChart = function() {
    const ctx = document.getElementById('productivityChart');
    if (!ctx) return;

    // Sample data - replace with real data from API
    const data = {
        labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
        datasets: [{
            label: 'Productivity %',
            data: [65, 72, 80, 74, 85, 78, 82],
            borderColor: 'rgba(102, 126, 234, 1)',
            backgroundColor: 'rgba(102, 126, 234, 0.1)',
            borderWidth: 3,
            fill: true,
            tension: 0.4,
            pointBackgroundColor: 'rgba(102, 126, 234, 1)',
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
            pointRadius: 6,
            pointHoverRadius: 8
        }]
    };

    const config = {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: 'rgba(102, 126, 234, 1)',
                    borderWidth: 1,
                    cornerRadius: 8,
                    displayColors: false,
                    callbacks: {
                        label: function(context) {
                            return `Productivity: ${context.parsed.y}%`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: '#6c757d',
                        font: {
                            size: 12
                        }
                    }
                },
                y: {
                    beginAtZero: true,
                    max: 100,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)',
                        borderDash: [5, 5]
                    },
                    ticks: {
                        color: '#6c757d',
                        font: {
                            size: 12
                        },
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            },
            elements: {
                point: {
                    hoverBackgroundColor: '#fff'
                }
            }
        }
    };

    new Chart(ctx, config);
};

// Initialize Application Usage Chart
window.initializeApplicationChart = function() {
    const ctx = document.getElementById('applicationChart');
    if (!ctx) return;

    // Sample data - replace with real data from API
    const data = {
        labels: ['VS Code', 'Chrome', 'Outlook', 'Teams', 'Excel', 'Other'],
        datasets: [{
            data: [30, 25, 15, 12, 8, 10],
            backgroundColor: [
                'rgba(102, 126, 234, 0.8)',
                'rgba(118, 75, 162, 0.8)',
                'rgba(17, 153, 142, 0.8)',
                'rgba(240, 147, 251, 0.8)',
                'rgba(79, 172, 254, 0.8)',
                'rgba(162, 155, 254, 0.8)'
            ],
            borderColor: [
                'rgba(102, 126, 234, 1)',
                'rgba(118, 75, 162, 1)',
                'rgba(17, 153, 142, 1)',
                'rgba(240, 147, 251, 1)',
                'rgba(79, 172, 254, 1)',
                'rgba(162, 155, 254, 1)'
            ],
            borderWidth: 2,
            hoverBorderWidth: 3,
            hoverOffset: 8
        }]
    };

    const config = {
        type: 'doughnut',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20,
                        font: {
                            size: 12
                        },
                        color: '#6c757d',
                        usePointStyle: true,
                        pointStyle: 'circle'
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: 'rgba(102, 126, 234, 1)',
                    borderWidth: 1,
                    cornerRadius: 8,
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = Math.round((value / total) * 100);
                            return `${label}: ${percentage}%`;
                        }
                    }
                }
            },
            cutout: '60%',
            elements: {
                arc: {
                    borderJoinStyle: 'round'
                }
            }
        }
    };

    new Chart(ctx, config);
};

// Real-time updates
window.updateDashboardData = function() {
    // This function will be called periodically to update charts with real data
    // Implementation will depend on your real-time data source (SignalR, polling, etc.)
    console.log('Updating dashboard data...');
};

// Auto-refresh functionality
let refreshInterval;

window.startAutoRefresh = function(intervalSeconds = 30) {
    if (refreshInterval) {
        clearInterval(refreshInterval);
    }
    
    refreshInterval = setInterval(() => {
        window.updateDashboardData();
    }, intervalSeconds * 1000);
};

window.stopAutoRefresh = function() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Start auto-refresh with 30-second interval
    window.startAutoRefresh(30);
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    window.stopAutoRefresh();
});

// Screenshot modal functionality
window.showScreenshotModal = function(screenshotUrl, employeeName, timestamp) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'screenshotModal';
    modal.tabIndex = -1;
    modal.innerHTML = `
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Screenshot - ${employeeName}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body text-center">
                    <img src="${screenshotUrl}" class="img-fluid rounded" alt="Screenshot">
                    <p class="text-muted mt-2">${timestamp}</p>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
    
    modal.addEventListener('hidden.bs.modal', function() {
        document.body.removeChild(modal);
    });
};

// Activity filter functionality
window.filterActivityByType = function(activityType) {
    const timelineItems = document.querySelectorAll('.timeline-item');
    
    timelineItems.forEach(item => {
        const activityTypeElement = item.querySelector('.text-muted');
        if (activityType === 'all' || activityTypeElement.textContent.includes(activityType)) {
            item.style.display = 'block';
        } else {
            item.style.display = 'none';
        }
    });
};

// Employee status toggle
window.toggleEmployeeDetails = function(employeeId) {
    const detailsElement = document.getElementById(`employee-details-${employeeId}`);
    if (detailsElement) {
        detailsElement.style.display = detailsElement.style.display === 'none' ? 'block' : 'none';
    }
};

// Search functionality
window.searchEmployees = function(searchTerm) {
    const employeeItems = document.querySelectorAll('.employee-status-item');
    
    employeeItems.forEach(item => {
        const employeeName = item.querySelector('h6').textContent.toLowerCase();
        const department = item.querySelector('small').textContent.toLowerCase();
        
        if (employeeName.includes(searchTerm.toLowerCase()) || 
            department.includes(searchTerm.toLowerCase()) ||
            searchTerm === '') {
            item.style.display = 'flex';
        } else {
            item.style.display = 'none';
        }
    });
};

// Notification system
window.showNotification = function(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        if (notification.parentNode) {
            notification.parentNode.removeChild(notification);
        }
    }, 5000);
};

// Export functionality
window.exportDashboardData = function(format = 'pdf') {
    window.showNotification(`Exporting dashboard data as ${format.toUpperCase()}...`, 'info');
    
    // Implementation for actual export functionality
    setTimeout(() => {
        window.showNotification(`Dashboard data exported successfully!`, 'success');
    }, 2000);
};

// Theme toggle
window.toggleTheme = function() {
    const body = document.body;
    const currentTheme = body.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    body.setAttribute('data-theme', newTheme);
    localStorage.setItem('dashboard-theme', newTheme);
    
    // Reinitialize charts with new theme colors
    setTimeout(() => {
        window.initializeProductivityChart();
        window.initializeApplicationChart();
    }, 100);
};

// Load saved theme
document.addEventListener('DOMContentLoaded', function() {
    const savedTheme = localStorage.getItem('dashboard-theme');
    if (savedTheme) {
        document.body.setAttribute('data-theme', savedTheme);
    }
}); 