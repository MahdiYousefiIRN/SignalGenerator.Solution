using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Web.Interfaces
{
    public interface ISignalProcessorService
    {
        Task StartSignalGeneration(SignalData config, IProtocolCommunication protocolCommunication);
        Task<List<SignalData>> GetSignalsAsync(SignalData config, IProtocolCommunication protocolCommunication);
        Task<bool> SendSignalsAsync(List<SignalData> signalData, IProtocolCommunication protocolCommunication);
        Task<bool> MonitorSignalStatus(IProtocolCommunication protocolCommunication);
    }
}
