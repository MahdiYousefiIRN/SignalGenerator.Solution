using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SignalGenerator.Core.Models;

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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // بررسی مقدار null
        }

        /// <summary>
        /// Tests the transmission of signals based on a configuration.
        /// </summary>
        /// <param name="config">The configuration for the signal test.</param>
        /// <returns>A TestResult object containing the results of the test.</returns>
        public async Task<TestResult> TestSignalTransmissionAsync(SignalData config)
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

        /// <summary>
        /// Generates test signals based on the configuration.
        /// </summary>
        /// <param name="config">The configuration for generating signals.</param>
        /// <returns>A list of generated SignalData objects.</returns>
        private async Task<List<SignalData>> GenerateTestSignalsAsync(SignalData config)
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

        /// <summary>
        /// Tests the transmission of signals using a specified protocol.
        /// </summary>
        /// <param name="signals">The list of signals to transmit.</param>
        /// <param name="result">The TestResult object to update with the results.</param>
        /// <param name="protocolType">The type of protocol to use for transmission.</param>
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

        /// <summary>
        /// Verifies the integrity of the signals.
        /// </summary>
        /// <param name="signals">The list of signals to verify.</param>
        /// <param name="result">The TestResult object to update with the results.</param>
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

        /// <summary>
        /// Starts monitoring the performance of an operation.
        /// </summary>
        /// <param name="operation">The name of the operation to monitor.</param>
        private void StartPerformanceMonitoring(string operation)
        {
            if (!_performanceTimers.ContainsKey(operation))
                _performanceTimers[operation] = Stopwatch.StartNew();
        }

        /// <summary>
        /// Stops monitoring the performance of an operation.
        /// </summary>
        /// <param name="operation">The name of the operation to stop monitoring.</param>
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

        /// <summary>
        /// Calculates performance metrics based on the execution times.
        /// </summary>
        /// <param name="result">The TestResult object to update with the metrics.</param>
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
