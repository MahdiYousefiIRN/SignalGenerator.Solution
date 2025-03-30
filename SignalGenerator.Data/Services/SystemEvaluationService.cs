using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Interfaces;

namespace SignalGenerator.Data.Services
{
    public class SystemEvaluationService : ISystemEvaluationService
    {
        private readonly ISignalTestingService _testingService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly ILogger<SystemEvaluationService> _logger;
        private readonly Dictionary<string, PerformanceMetric> _performanceMetrics;
        private readonly object _lockObject = new object();

        public SystemEvaluationService(
            ISignalTestingService testingService,
            IErrorHandlingService errorHandlingService,
            ILogger<SystemEvaluationService> logger)
        {
            _testingService = testingService ?? throw new ArgumentNullException(nameof(testingService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMetrics = new Dictionary<string, PerformanceMetric>();
        }

        public async Task<EvaluationResult> EvaluateSystemAsync(EvaluationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var result = new EvaluationResult
            {
                StartTime = DateTime.UtcNow,
                Config = config,
                InitialStatus = new SystemStatus(),
                ProtocolResults = new Dictionary<string, TestResult>(),
                IntegrityResults = new Dictionary<string, IntegrityResult>(),
                PerformanceAnalysis = new Dictionary<string, PerformanceAnalysis>()
            };

            try
            {
                // Protocol-specific tests
                foreach (var protocol in config.Protocols)
                {
                    await TestProtocolAsync(protocol, result);
                }

                // Load testing
                if (config.PerformLoadTest)
                {
                    await PerformLoadTestAsync(result);
                }

                // Signal integrity verification
                await VerifySignalIntegrityAsync(result);

                // Performance analysis
                AnalyzePerformance(result);

                result.Success = true;
                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _errorHandlingService.LogError("SystemEvaluation", ex);
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorDetails = ex.ToString();
            }

            return result;
        }

        private async Task TestProtocolAsync(string protocol, EvaluationResult result)
        {
            if (string.IsNullOrEmpty(protocol))
                throw new ArgumentException("Protocol cannot be null or empty", nameof(protocol));

            var protocolConfig = new SignalConfig
            {
                SignalCount = result.Config.SignalCount,
                MinFrequency = result.Config.MinFrequency,
                MaxFrequency = result.Config.MaxFrequency,
                Interval = result.Config.Interval,
                ProtocolType = protocol
            };

            var testResult = await _testingService.TestSignalTransmissionAsync(protocolConfig);
            result.ProtocolResults[protocol] = testResult;

            // Track performance metrics
            foreach (var metric in testResult.PerformanceMetrics)
            {
                TrackPerformanceMetric($"{protocol}_{metric.Operation}", metric.TotalDuration);
            }
        }

        private async Task PerformLoadTestAsync(EvaluationResult result)
        {
            var loadTestConfig = new SignalConfig
            {
                SignalCount = result.Config.SignalCount * 5, // 5x normal load
                MinFrequency = result.Config.MinFrequency,
                MaxFrequency = result.Config.MaxFrequency,
                Interval = result.Config.Interval / 2, // Faster interval
                ProtocolType = result.Config.Protocols.First()
            };

            var loadTestResult = await _testingService.TestSignalTransmissionAsync(loadTestConfig);
            result.LoadTestResult = loadTestResult;

            // Track load test performance metrics
            foreach (var metric in loadTestResult.PerformanceMetrics)
            {
                TrackPerformanceMetric($"load_test_{metric.Operation}", metric.TotalDuration);
            }
        }

        private Task VerifySignalIntegrityAsync(EvaluationResult result)
        {
            foreach (var protocolResult in result.ProtocolResults.Values)
            {
                var integrityChecks = protocolResult.IntegrityChecks;
                result.IntegrityResults[protocolResult.Config.ProtocolType] = new IntegrityResult
                {
                    TotalChecks = integrityChecks.Count,
                    SuccessfulChecks = integrityChecks.Count(c => c.Success),
                    FailedChecks = integrityChecks.Count(c => !c.Success),
                    SuccessRate = integrityChecks.Count > 0
                        ? (double)integrityChecks.Count(c => c.Success) / integrityChecks.Count * 100
                        : 0
                };
            }
            return Task.CompletedTask;
        }

        private void AnalyzePerformance(EvaluationResult result)
        {
            foreach (var metric in _performanceMetrics)
            {
                result.PerformanceAnalysis[metric.Key] = new PerformanceAnalysis
                {
                    AverageDuration = metric.Value.AverageDuration,
                    MaxDuration = metric.Value.MaxDuration,
                    MinDuration = metric.Value.MinDuration,
                    TotalCalls = metric.Value.TotalCalls
                };
            }
        }

        private void TrackPerformanceMetric(string operation, long duration)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));

            lock (_lockObject)
            {
                if (!_performanceMetrics.ContainsKey(operation))
                {
                    _performanceMetrics[operation] = new PerformanceMetric
                    {
                        Operation = operation,
                        TotalDuration = duration,
                        MaxDuration = duration,
                        MinDuration = duration,
                        TotalCalls = 1
                    };
                }
                else
                {
                    var metric = _performanceMetrics[operation];
                    metric.TotalDuration += duration;
                    metric.MaxDuration = Math.Max(metric.MaxDuration, duration);
                    metric.MinDuration = Math.Min(metric.MinDuration, duration);
                    metric.TotalCalls++;
                    metric.AverageDuration = metric.TotalDuration / metric.TotalCalls;
                }
            }
        }
    }

    public interface ISystemEvaluationService
    {
        Task<EvaluationResult> EvaluateSystemAsync(EvaluationConfig config);
    }

    public class EvaluationConfig
    {
        public int SignalCount { get; set; } = 10;
        public double MinFrequency { get; set; } = 0;
        public double MaxFrequency { get; set; } = 1000;
        public int Interval { get; set; } = 1000;
        public List<string> Protocols { get; set; } = new List<string> { "http", "modbus", "signalr" };
        public bool PerformLoadTest { get; set; } = true;
    }

    public class EvaluationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public required EvaluationConfig Config { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorDetails { get; set; } = string.Empty;
        public required SystemStatus InitialStatus { get; set; }
        public Dictionary<string, TestResult> ProtocolResults { get; set; } = new();
        public TestResult? LoadTestResult { get; set; }
        public Dictionary<string, IntegrityResult> IntegrityResults { get; set; } = new();
        public Dictionary<string, PerformanceAnalysis> PerformanceAnalysis { get; set; } = new();
    }

    public class IntegrityResult
    {
        public int TotalChecks { get; set; }
        public int SuccessfulChecks { get; set; }
        public int FailedChecks { get; set; }
        public double SuccessRate { get; set; }
    }

    public class PerformanceAnalysis
    {
        public long AverageDuration { get; set; }
        public long MaxDuration { get; set; }
        public long MinDuration { get; set; }
        public int TotalCalls { get; set; }
    }

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