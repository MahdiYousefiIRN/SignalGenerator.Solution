using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class SignalConfig
    {
        /// <summary>
        /// Unique identifier for the configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The number of signals to generate.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Signal count must be between 1 and 1000.")]
        public int SignalCount { get; set; }

        /// <summary>
        /// The minimum frequency for the signals.
        /// </summary>
        [Range(40, 70, ErrorMessage = "Minimum frequency must be between 40 and 70.")]
        public double MinFrequency { get; set; }

        /// <summary>
        /// The maximum frequency for the signals.
        /// </summary>
        [Range(40, 70, ErrorMessage = "Maximum frequency must be between 40 and 70.")]
        public double MaxFrequency { get; set; }

        /// <summary>
        /// The interval between signals.
        /// </summary>
        [Range(1, 1440, ErrorMessage = "Interval must be between 1 and 1440.")]
        public int Interval { get; set; }

        /// <summary>
        /// The interval in milliseconds.
        /// </summary>
        public int IntervalMs { get; set; }

        /// <summary>
        /// The type of protocol used for the signals.
        /// </summary>
        [Required(ErrorMessage = "Protocol type is required.")]
        public required string ProtocolType { get; set; }

        /// <summary>
        /// The ID of the user who created the configuration.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// The timestamp when the configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
} 