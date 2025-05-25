using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpAnalysis.Shared.Models
{
    /// <summary>
    /// Provides advanced analytics: risk scoring, anomaly detection, and trend analysis for employee activity.
    /// </summary>
    public class AdvancedAnalyticsService
    {
        public double CalculateRiskScore(List<ApplicationUsage> appUsages, List<WebsiteVisit> webVisits, List<SystemEvent> events)
        {
            double unproductiveMinutes = appUsages.Where(a => !a.IsProductiveApplication).Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);
            double securityEvents = events.Count(e => e.EventType == SystemEventType.USBInsert || e.EventType == SystemEventType.USBRemove || e.EventType == SystemEventType.NetworkActivity);
            double anomalyScore = DetectAnomalies(appUsages, webVisits, events).Count * 2;
            double risk = unproductiveMinutes * 0.5 + securityEvents * 5 + anomalyScore;
            return Math.Min(risk, 100);
        }

        public List<string> DetectAnomalies(List<ApplicationUsage> appUsages, List<WebsiteVisit> webVisits, List<SystemEvent> events)
        {
            var anomalies = new List<string>();
            var rareApps = appUsages.GroupBy(a => a.ApplicationName)
                .Where(g => g.Count() == 1)
                .Select(g => g.Key);
            anomalies.AddRange(rareApps.Select(a => $"Rare application used: {a}"));
            var offHours = appUsages.Where(a => a.StartTime.Hour < 7 || a.StartTime.Hour > 20);
            if (offHours.Any())
                anomalies.Add("Activity detected outside normal working hours");
            if (events.Any(e => e.EventType == SystemEventType.USBInsert || e.EventType == SystemEventType.USBRemove))
                anomalies.Add("USB device usage detected");
            return anomalies;
        }

        public List<(DateTime Period, double ProductivityScore)> AnalyzeTrends(List<ApplicationUsage> appUsages, TimeSpan periodSpan)
        {
            var trends = new List<(DateTime, double)>();
            if (!appUsages.Any()) return trends;
            var minDate = appUsages.Min(a => a.StartTime);
            var maxDate = appUsages.Max(a => a.EndTime ?? a.StartTime);
            for (var periodStart = minDate.Date; periodStart < maxDate; periodStart = periodStart.Add(periodSpan))
            {
                var periodEnd = periodStart.Add(periodSpan);
                var periodApps = appUsages.Where(a => a.StartTime >= periodStart && a.StartTime < periodEnd).ToList();
                if (periodApps.Count == 0) continue;
                double total = periodApps.Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);
                double productive = periodApps.Where(a => a.IsProductiveApplication).Sum(a => a.Duration.HasValue ? a.Duration.Value.TotalMinutes : 0);
                double score = total > 0 ? (productive / total) * 100 : 0;
                trends.Add((periodStart, score));
            }
            return trends;
        }
    }
}
