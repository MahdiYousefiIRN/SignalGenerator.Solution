
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class TestResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public SignalData Config { get; set; }
        public int SignalsGenerated { get; set; }
        public List<TransmissionResult> HttpTransmissions { get; set; }
        public List<TransmissionResult> ModbusTransmissions { get; set; }
        public List<TransmissionResult> SignalRTransmissions { get; set; }
        public List<IntegrityCheck> IntegrityChecks { get; set; }
        public List<ErrorDetail> HttpErrors { get; set; }
        public List<ErrorDetail> ModbusErrors { get; set; }
        public List<ErrorDetail> SignalRErrors { get; set; }
        public List<ErrorDetail> IntegrityErrors { get; set; }
        public List<PerformanceMetric> PerformanceMetrics { get; set; }
    }
}
