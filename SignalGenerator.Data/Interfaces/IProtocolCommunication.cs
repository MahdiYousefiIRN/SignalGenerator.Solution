using SignalGenerator.Core.Models;
using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolCommunication
    {
        /// <summary>
        /// Receives signals based on the specified configuration.
        /// </summary>
        /// <param name="config">The configuration for receiving signals.</param>
        /// <returns>A list of SignalData objects.</returns>
        Task<List<SignalData>> ReceiveSignalsAsync(SignalData config);

        /// <summary>
        /// Sends a list of signals.
        /// </summary>
        /// <param name="signalData">The list of signals to send.</param>
        /// <returns>A boolean indicating whether the signals were sent successfully.</returns>
        Task<bool> SendSignalsAsync(List<SignalData> signalData);

        /// <summary>
        /// Monitors the status of the protocol communication.
        /// </summary>
        /// <returns>A boolean indicating whether the monitoring is successful.</returns>
        Task<bool> MonitorStatusAsync();
    }
} 