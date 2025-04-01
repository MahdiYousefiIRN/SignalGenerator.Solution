using SignalGenerator.Data.Services;

namespace SignalGenerator.Data.Interfaces
{
    public interface IErrorHandlingService
    {
        // متد برای ثبت خطا
        void LogError(string component, Exception ex, string? context = null);

        // متد برای ثبت هشدار
        void LogWarning(string component, string message, string? context = null);

        // متد برای دریافت وضعیت سیستم (مثلاً استفاده از معیارهای عملکرد)
        SystemStatus GetSystemStatus();

        // متد برای دریافت خطاهای اخیر با امکان فیلتر کردن
        Task<List<ErrorEvent>> GetErrorsAsync(string? component = null, int count = 100, bool includeWarnings = false);
    }
}
