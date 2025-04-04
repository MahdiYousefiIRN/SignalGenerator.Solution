using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class SignalData
    {
        /// <summary>
        /// Unique identifier for the signal.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }           
        /// <summary>
        /// The frequency of the signal.
        /// </summary>
        [Range(40, 70, ErrorMessage = "Frequency must be between 40 and 70.")]
        public double Frequency { get; set; }

        /// <summary>
        /// The power of the signal.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Power must be between 0 and 100.")]
        public double Power { get; set; }

        /// <summary>
        /// The timestamp of the signal.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The type of protocol used for the signal.
        /// </summary>
        [Required(ErrorMessage = "Protocol type is required.")]
        public required string ProtocolType { get; set; }

        /// <summary>
        /// A boolean indicating the status of the coil.
        /// </summary>
        public bool CoilStatus { get; set; }

        /// <summary>
        /// A boolean indicating the status of the discrete input.
        /// </summary>
        public bool DiscreteInputStatus { get; set; }
        /// <summary>
        /// The number of signals to generate.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Signal count must be between 1 and 1000.")]
        public int SignalCount { get; set; }
        public int MaxFrequency { get;  set; }
        public int MinFrequency { get;  set; }
        public int IntervalMs { get;  set; }
        public DateTime CreatedAt { get; set; }
    }
} 