using EmpAnalysis.Agent.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpAnalysis.Agent.Services
{
    public class ProductivityService
    {
        // Calculates a productivity score for a given period
        public double CalculateScore(List<ApplicationUsage> appUsages, List<WebsiteVisit> webVisits, TimeSpan totalActive, TimeSpan totalIdle)
        {
            // Example: 60% app productivity, 30% web, 10% idle penalty
            double appScore = appUsages.Where(a => a.IsProductiveApp).Sum(a => a.Duration.TotalMinutes);
            double webScore = webVisits.Where(w => w.IsProductiveSite).Sum(w => w.Duration.TotalMinutes);
            double totalMinutes = totalActive.TotalMinutes + totalIdle.TotalMinutes;
            if (totalMinutes == 0) return 0;
            double idlePenalty = totalIdle.TotalMinutes / totalMinutes;
            double score = (0.6 * appScore + 0.3 * webScore) / totalMinutes * (1 - 0.1 * idlePenalty);
            return Math.Round(score * 100, 2); // Return as percentage
        }

        // Calculates a productivity score for a given period (improved version)
        public double CalculateScore(List<ApplicationUsage> appUsages, List<WebsiteVisit> webVisits, List<SystemEvent> events, TimeSpan period, TimeSpan workingHours)
        {
            // Calculate idle time from events
            var idleEvents = events.Where(e => e.EventType == SystemEventType.IdleStart || e.EventType == SystemEventType.IdleEnd).OrderBy(e => e.Timestamp).ToList();
            TimeSpan idleTime = TimeSpan.Zero;
            for (int i = 0; i < idleEvents.Count - 1; i += 2)
            {
                idleTime += idleEvents[i + 1].Timestamp - idleEvents[i].Timestamp;
            }
            var activeTime = period - idleTime;

            // Productive app time
            var productiveAppTime = appUsages.Where(a => a.IsProductiveApp).Sum(a => a.Duration.TotalMinutes);
            // Productive website time
            var productiveWebTime = webVisits.Where(w => w.IsProductiveSite).Sum(w => w.Duration.TotalMinutes);
            // Total monitored time
            var totalMonitored = appUsages.Sum(a => a.Duration.TotalMinutes) + webVisits.Sum(w => w.Duration.TotalMinutes);
            if (totalMonitored == 0) return 0;

            // Simple productivity score: (productive time / total monitored time) * (active time / working hours)
            var productiveTime = productiveAppTime + productiveWebTime;
            var score = (productiveTime / totalMonitored) * (activeTime.TotalMinutes / workingHours.TotalMinutes);
            return Math.Round(score * 100, 2); // Return as percentage
        }
    }
}
