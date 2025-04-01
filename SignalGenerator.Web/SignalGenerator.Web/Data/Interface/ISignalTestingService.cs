using SignalGenerator.Core.Models;

namespace SignalGenerator.Data.Interfaces
{
    public interface ISignalTestingService
    {
        Task<TestResult> TestSignalTransmissionAsync(SignalData config);
        Task<string> GetTestStatusAsync();  // وضعیت تست
        Task<List<string>> GetErrorsAsync();  // خطاها
    }
}
