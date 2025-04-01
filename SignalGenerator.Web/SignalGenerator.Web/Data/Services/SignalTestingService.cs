namespace SignalGenerator.Data.Services
{
    using SignalGenerator.Core.Models;
    using SignalGenerator.Data.Interfaces;
    using SignalGenerator.Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SignalTestingService : ISignalTestingService
    {
        private static string currentTestStatus = "Test not started";  // وضعیت تست فعلی
        private static List<string> errors = new List<string>();  // ذخیره‌سازی خطاها

        public async Task<TestResult> TestSignalTransmissionAsync(SignalData config)
        {
            // فرضی: سیگنال را تست می‌کنیم و نتیجه را باز می‌گردانیم
            currentTestStatus = "Test in progress";

            // ایجاد نتیجه تست
            var result = new TestResult
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(5),
                Success = true, // فرضی
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

            // فرضی: نتیجه را باز می‌گردانیم
            return await Task.FromResult(result);
        }

        public async Task<string> GetTestStatusAsync()
        {
            // وضعیت تست را باز می‌گردانیم
            return await Task.FromResult(currentTestStatus);
        }

        public async Task<List<string>> GetErrorsAsync()
        {
            // خطاهای ذخیره‌شده را باز می‌گردانیم
            return await Task.FromResult(errors);
        }

        // متد برای شبیه‌سازی اضافه کردن خطا
        public void AddError(string errorMessage)
        {
            errors.Add(errorMessage);
        }
    }
}
