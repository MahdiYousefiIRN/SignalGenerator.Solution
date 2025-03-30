using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Interfaces
{
    public interface ISignalTestingService
    {
        Task<TestResult> TestSignalTransmissionAsync(SignalConfig config);
    }

    public class TestResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public required SignalConfig Config { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorDetails { get; set; } = string.Empty;
        public int SignalsGenerated { get; set; }
        public List<TransmissionResult> HttpTransmissions { get; set; } = new();
        public List<TransmissionResult> ModbusTransmissions { get; set; } = new();
        public List<TransmissionResult> SignalRTransmissions { get; set; } = new();
        public List<IntegrityCheck> IntegrityChecks { get; set; } = new();
        public List<ErrorDetail> HttpErrors { get; set; } = new();
        public List<ErrorDetail> ModbusErrors { get; set; } = new();
        public List<ErrorDetail> SignalRErrors { get; set; } = new();
        public List<ErrorDetail> IntegrityErrors { get; set; } = new();
        public List<PerformanceMetric> PerformanceMetrics { get; set; } = new();  // 👈 این باید وجود داشته باشد
    }


    public class TransmissionResult
    {
        public required string SignalId { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class IntegrityCheck
    {
        public required string SignalId { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ErrorDetail
    {
        public required string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
