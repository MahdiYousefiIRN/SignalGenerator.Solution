using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class PerformanceMetric
    {
        /// <summary>
        /// The name of the operation being measured.
        /// </summary>
        public required string Operation { get; set; }

        /// <summary>
        /// The total duration of the operation.
        /// </summary>
        public long TotalDuration { get; set; }

        /// <summary>
        /// The maximum duration of the operation.
        /// </summary>
        public long MaxDuration { get; set; }

        /// <summary>
        /// The minimum duration of the operation.
        /// </summary>
        public long MinDuration { get; set; }

        /// <summary>
        /// The total number of calls made to the operation.
        /// </summary>
        public int TotalCalls { get; set; }

        /// <summary>
        /// The average duration of the operation.
        /// </summary>
        public long AverageDuration { get; set; }
    }
} 