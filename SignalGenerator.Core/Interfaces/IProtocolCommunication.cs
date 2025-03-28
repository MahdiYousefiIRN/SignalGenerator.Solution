using SignalGenerator.Core.Models;

namespace SignalGenerator.Core.Interfaces
{
    public interface IProtocolCommunication
    {
        Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config);
        Task<bool> SendSignalsAsync(List<SignalData> signalData);
        Task<bool> MonitorStatusAsync();
    }
}
