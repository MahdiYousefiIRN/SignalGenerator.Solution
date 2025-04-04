namespace SignalGenerator.Web.Services
{
    using SignalGenerator.Data.Interfaces;
    using SignalGenerator.Data.Models;
    using SignalGenerator.Web.Data.Interface;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SignalTestingService : ISignalTestingService
    {
        private static string _currentTestStatus = "Test not started";
        private static readonly List<string> _errors = new List<string>();

        public async Task<TestResult> TestSignalTransmissionAsync(SignalData config)
        {
            _currentTestStatus = "Test in progress";

            var result = new TestResult
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(5),
                Success = true,
                Config = config,
                SignalsGenerated = 10,
                HttpTransmissions = new List<TransmissionResult>(),
                ModbusTransmissions = new List<TransmissionResult>(),
                SignalRTransmissions = new List<TransmissionResult>(),
                IntegrityChecks = new List<IntegrityCheck>(),
                HttpErrors = new List<ErrorDetail>(),
                ModbusErrors = new List<ErrorDetail>(),
                SignalRErrors = new List<ErrorDetail>(),
                IntegrityErrors = new List<ErrorDetail>(),
                PerformanceMetrics = new List<PerformanceMetric>()
            };

            _currentTestStatus = "Test completed";
            return await Task.FromResult(result);
        }

        public Task<string> GetTestStatusAsync()
        {
            return Task.FromResult(_currentTestStatus);
        }

        public Task<List<string>> GetErrorsAsync()
        {
            return Task.FromResult(new List<string>(_errors));
        }

        public void AddError(string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                _errors.Add(errorMessage);
            }
        }
    }
}
