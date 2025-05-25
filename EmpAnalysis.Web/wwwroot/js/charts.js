// EmpAnalysis Dashboard Charts
// Professional data visualization using Chart.js

// Global Chart.js configuration
Chart.defaults.font.family = "'Inter', 'Segoe UI', sans-serif";
Chart.defaults.font.size = 12;
Chart.defaults.color = '#6B7280';

// Initialize Productivity Analytics Chart
window.initializeProductivityChart = function(data) {
    const ctx = document.getElementById('productivityChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#ffffff',
                    bodyColor: '#ffffff',
                    borderColor: '#2563EB',
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    displayColors: true
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(107, 114, 128, 0.1)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value + 'h';
                        }
                    }
                },
                x: {
                    grid: {
                        color: 'rgba(107, 114, 128, 0.1)'
                    }
                }
            },
            elements: {
                point: {
                    radius: 6,
                    hoverRadius: 8,
                    borderWidth: 2,
                    backgroundColor: '#ffffff'
                },
                line: {
                    borderWidth: 3
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });
};

// Initialize Employee Status Chart (Doughnut)
window.initializeStatusChart = function(data) {
    const ctx = document.getElementById('statusChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 20,
                        generateLabels: function(chart) {
                            const data = chart.data;
                            if (data.labels.length && data.datasets.length) {
                                return data.labels.map((label, i) => {
                                    const dataset = data.datasets[0];
                                    const backgroundColor = dataset.backgroundColor[i];
                                    const value = dataset.data[i];
                                    
                                    return {
                                        text: `${label}: ${value}`,
                                        fillStyle: backgroundColor,
                                        strokeStyle: backgroundColor,
                                        lineWidth: 0,
                                        pointStyle: 'circle'
                                    };
                                });
                            }
                            return [];
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#ffffff',
                    bodyColor: '#ffffff',
                    borderColor: '#2563EB',
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((context.raw / total) * 100).toFixed(1);
                            return `${context.label}: ${context.raw} (${percentage}%)`;
                        }
                    }
                }
            },
            elements: {
                arc: {
                    borderWidth: 0
                }
            }
        }
    });
};

// Initialize Applications Usage Chart
window.initializeApplicationsChart = function(data) {
    const ctx = document.getElementById('applicationsChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'y',
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#ffffff',
                    bodyColor: '#ffffff',
                    borderColor: '#2563EB',
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    callbacks: {
                        label: function(context) {
                            return `${context.parsed.x.toFixed(1)} hours`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(107, 114, 128, 0.1)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value + 'h';
                        }
                    }
                },
                y: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        maxTicksLimit: 6
                    }
                }
            },
            elements: {
                bar: {
                    borderRadius: 6,
                    borderSkipped: false
                }
            }
        }
    });
};

// Initialize Real-time Activity Timeline
window.initializeActivityTimeline = function(activities) {
    // Custom implementation for activity timeline
    const container = document.querySelector('.activity-feed');
    if (!container) return;

    // Add real-time animation effects
    const items = container.querySelectorAll('.activity-item');
    items.forEach((item, index) => {
        item.style.animationDelay = `${index * 0.1}s`;
        item.classList.add('fade-in');
    });
};

// Initialize System Health Gauges
window.initializeHealthGauges = function() {
    const healthBars = document.querySelectorAll('.health-progress');
    
    healthBars.forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0%';
        
        setTimeout(() => {
            bar.style.transition = 'width 1.5s ease-out';
            bar.style.width = width;
        }, 300);
    });
};

// Utility function to update charts with real-time data
window.updateChartData = function(chartId, newData) {
    const chart = Chart.getChart(chartId);
    if (chart) {
        chart.data = newData;
        chart.update('animate');
    }
};

// Auto-refresh functionality
let refreshInterval;

window.startAutoRefresh = function(intervalMs = 30000) {
    refreshInterval = setInterval(() => {
        // Trigger Blazor component refresh
        DotNet.invokeMethodAsync('EmpAnalysis.Web', 'RefreshDashboard');
    }, intervalMs);
};

window.stopAutoRefresh = function() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
};

// Initialize all charts when page loads
document.addEventListener('DOMContentLoaded', function() {
    // Add smooth animations
    const cards = document.querySelectorAll('.metric-card, .content-section');
    cards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
        card.classList.add('fade-in-up');
    });
    
    // Initialize health gauges
    setTimeout(() => {
        initializeHealthGauges();
    }, 500);
});

// CSS Animation classes
const style = document.createElement('style');
style.textContent = `
    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }
    
    @keyframes fadeInUp {
        from { opacity: 0; transform: translateY(20px); }
        to { opacity: 1; transform: translateY(0); }
    }
    
    .fade-in {
        animation: fadeIn 0.6s ease-out forwards;
    }
    
    .fade-in-up {
        animation: fadeInUp 0.8s ease-out forwards;
    }
    
    .metric-card {
        opacity: 0;
    }
    
    .content-section {
        opacity: 0;
    }
`;
document.head.appendChild(style); 