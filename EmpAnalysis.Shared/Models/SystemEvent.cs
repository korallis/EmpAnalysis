using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpAnalysis.Shared.Models
{
    /// <summary>
    /// Shared system event model for analytics.
    /// </summary>
    public class SystemEvent
    {
        public SystemEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public enum SystemEventType
    {
        Unknown = 0,
        USBInsert = 1,
        USBRemove = 2,
        NetworkActivity = 3,
        SystemEvent = 4
    }
}
