using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class PerformanceMetric
    {
        public required string Operation { get; set; }
        public long TotalDuration { get; set; }
        public long MaxDuration { get; set; }
        public long MinDuration { get; set; }
        public int TotalCalls { get; set; }
        public long AverageDuration { get; set; }
    }
} 