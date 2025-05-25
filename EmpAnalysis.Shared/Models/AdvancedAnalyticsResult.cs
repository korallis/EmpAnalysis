using System;
using System.Collections.Generic;

namespace EmpAnalysis.Shared.Models
{
    /// <summary>
    /// Represents advanced analytics results for the dashboard (risk, anomalies, trends).
    /// </summary>
    public class AdvancedAnalyticsResult
    {
        public double RiskScore { get; set; }
        public List<string> Anomalies { get; set; } = new();
        public List<TrendPoint> Trends { get; set; } = new();
    }

    public class TrendPoint
    {
        public DateTime Period { get; set; }
        public double ProductivityScore { get; set; }
    }
}
