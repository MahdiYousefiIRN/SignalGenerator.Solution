using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class SignalConfig
    {
        public int Id { get; set; }
        public int SignalCount { get; set; }
        public double MinFrequency { get; set; }
        public double MaxFrequency { get; set; }
        public int Interval { get; set; }
        public int IntervalMs { get; set; }
        public required string ProtocolType { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 