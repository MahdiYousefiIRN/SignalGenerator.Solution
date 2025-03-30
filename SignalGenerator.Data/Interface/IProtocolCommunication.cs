using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolCommunication
    {
        Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config);
        Task<bool> SendSignalsAsync(List<SignalData> signalData);
        Task<bool> MonitorStatusAsync();
    }
}
