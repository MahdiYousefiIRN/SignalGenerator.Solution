using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolCommunication
    {
        /// <summary>
        /// دریافت سیگنال‌ها بر اساس پیکربندی مشخص‌شده.
        /// </summary>
        Task<List<SignalData>> ReceiveSignalsAsync(SignalData config);

        /// <summary>
        /// ارسال لیستی از سیگنال‌ها.
        /// </summary>
        Task<bool> SendSignalsAsync(List<SignalData> signalData);

        /// <summary>
        /// مانیتور کردن وضعیت ارتباط با پروتکل.
        /// </summary>
        Task<bool> MonitorStatusAsync();
    }
}
