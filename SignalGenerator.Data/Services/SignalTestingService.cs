using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SignalGenerator.Data.Services
{
    public class SignalTestingService : ISignalTestingService
    {
        private readonly IProtocolDataStore _dataStore;
        private readonly ILogger<SignalTestingService> _logger;
        private readonly Dictionary<string, Stopwatch> _performanceTimers = new();
        private readonly Dictionary<string, List<long>> _executionTimes = new(); // ذخیره‌ی زمان‌های اجرا

        public SignalTestingService(IProtocolDataStore dataStore, ILogger<SignalTestingService> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<TestResult> TestSignalTransmissionAsync(SignalConfig config)
        {
            var result = new TestResult { StartTime = DateTime.UtcNow, Config = config };
            try
            {
                StartPerformanceMonitoring("signal_generation");
                var signals = await GenerateTestSignalsAsync(config);
                result.SignalsGenerated = signals.Count;
                StopPerformanceMonitoring("signal_generation");

                await TestTransmissionAsync(signals, result, config.ProtocolType);
                await VerifySignalIntegrityAsync(signals, result);
                CalculatePerformanceMetrics(result);

                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signal testing");
                result.Success = false;
                result.Error = ex.Message;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }
            return result;
        }

        private async Task<List<SignalData>> GenerateTestSignalsAsync(SignalConfig config)
        {
            return await Task.Run(() => Enumerable.Range(0, config.SignalCount).Select(_ => new SignalData
            {
                Id = Guid.NewGuid().ToString(),
                Frequency = Random.Shared.NextDouble() * (config.MaxFrequency - config.MinFrequency) + config.MinFrequency,
                Power = Random.Shared.NextDouble() * 100,
                Timestamp = DateTime.UtcNow,
                ProtocolType = config.ProtocolType,
                CoilStatus = Random.Shared.Next(2) == 1,
                DiscreteInputStatus = Random.Shared.Next(2) == 1
            }).ToList());
        }

        private async Task TestTransmissionAsync(List<SignalData> signals, TestResult result, string protocolType)
        {
            StartPerformanceMonitoring(protocolType + "_transmission");
            try
            {
                var transmissionResults = await Task.WhenAll(signals.Select(signal => _dataStore.SendSignalAsync(signal, protocolType)));
                var transmissions = signals.Zip(transmissionResults, (signal, success) => new TransmissionResult
                {
                    SignalId = signal.Id,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                }).ToList();

                switch (protocolType.ToLower())
                {
                    case "http":
                        result.HttpTransmissions.AddRange(transmissions);
                        break;
                    case "modbus":
                        result.ModbusTransmissions.AddRange(transmissions);
                        break;
                    case "signalar":
                        result.SignalRTransmissions.AddRange(transmissions);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during {protocolType} transmission");

                var errorDetail = new ErrorDetail { Message = ex.Message, Timestamp = DateTime.UtcNow };

                switch (protocolType.ToLower())
                {
                    case "http":
                        result.HttpErrors.Add(errorDetail);
                        break;
                    case "modbus":
                        result.ModbusErrors.Add(errorDetail);
                        break;
                    case "signalar":
                        result.SignalRErrors.Add(errorDetail);
                        break;
                }
            }
            finally
            {
                StopPerformanceMonitoring(protocolType + "_transmission");
            }
        }

        private async Task VerifySignalIntegrityAsync(List<SignalData> signals, TestResult result)
        {
            StartPerformanceMonitoring("integrity_verification");
            try
            {
                var verificationTasks = signals.Select(async signal =>
                {
                    var storedSignal = await _dataStore.GetSignalAsync(signal.Id);
                    return new IntegrityCheck
                    {
                        SignalId = signal.Id,
                        Success = storedSignal != null && storedSignal.Frequency == signal.Frequency &&
                                  storedSignal.Power == signal.Power && storedSignal.ProtocolType == signal.ProtocolType,
                        Timestamp = DateTime.UtcNow
                    };
                });
                result.IntegrityChecks = (await Task.WhenAll(verificationTasks)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during integrity verification");
                result.IntegrityErrors.Add(new ErrorDetail { Message = ex.Message, Timestamp = DateTime.UtcNow });

            }
            finally
            {
                StopPerformanceMonitoring("integrity_verification");
            }
        }

        private void StartPerformanceMonitoring(string operation)
        {
            if (!_performanceTimers.ContainsKey(operation))
                _performanceTimers[operation] = Stopwatch.StartNew();
        }

        private void StopPerformanceMonitoring(string operation)
        {
            if (_performanceTimers.TryGetValue(operation, out var stopwatch) && stopwatch.IsRunning)
            {
                stopwatch.Stop();

                if (!_executionTimes.ContainsKey(operation))
                    _executionTimes[operation] = new List<long>();

                _executionTimes[operation].Add(stopwatch.ElapsedMilliseconds);
            }
        }

        private void CalculatePerformanceMetrics(TestResult result)
        {
            if (!_executionTimes.Any())
            {
                _logger.LogWarning("No performance metrics found to calculate.");
                result.PerformanceMetrics = new List<SignalGenerator.Data.Models.PerformanceMetric>();
                return;
            }

            result.PerformanceMetrics = _executionTimes.Select(entry =>
            {
                var operation = entry.Key;
                var durations = entry.Value;
                var count = durations.Count;

                if (count == 0)
                {
                    return new SignalGenerator.Data.Models.PerformanceMetric
                    {
                        Operation = operation,
                        TotalDuration = 0,
                        MaxDuration = 0,
                        MinDuration = 0,
                        TotalCalls = 0,
                        AverageDuration = 0
                    };
                }

                var totalDuration = durations.Sum();

                return new SignalGenerator.Data.Models.PerformanceMetric
                {
                    Operation = operation,
                    TotalDuration = totalDuration,
                    MaxDuration = durations.Max(),
                    MinDuration = durations.Min(),
                    TotalCalls = count,
                    AverageDuration = totalDuration / count
                };
            }).ToList();
        }

    }
}
