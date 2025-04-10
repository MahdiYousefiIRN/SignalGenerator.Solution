using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.SignalProtocol;
using SignalGenerator.Helpers;
using SignalGenerator.Web.Interfaces;

public class SignalProcessorService : ISignalProcessorService
{
    private readonly IEnumerable<IProtocolCommunication> _protocols;
    private readonly ILoggerService _logger;

    public SignalProcessorService(IEnumerable<IProtocolCommunication> protocols, ILoggerService logger)
    {
        _protocols = protocols ?? throw new ArgumentNullException(nameof(protocols));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IProtocolCommunication? GetSignalRProtocol()
        => _protocols.OfType<SignalRProtocol>().FirstOrDefault();

    public async Task StartSignalGeneration(SignalData config, IProtocolCommunication protocolCommunication)
    {
        await _logger.LogInfo($"🚀 StartSignalGeneration: Initiating for '{config.Name}'");

        try
        {
            var signals = await protocolCommunication.ReceiveSignalsAsync(config);
            await _logger.LogInfo($"📥 Received {signals.Count} signals from protocol.");

            bool sent = await protocolCommunication.SendSignalsAsync(signals);
            if (!sent)
                await _logger.LogWarning("⚠️ Primary protocol failed to send signals.");
            else
                await _logger.LogInfo("✅ Signals sent successfully via primary protocol.");

            var signalR = GetSignalRProtocol();
            if (signalR != null)
            {
                bool signalRSent = await signalR.SendSignalsAsync(signals);
                await _logger.LogInfo(signalRSent
                    ? "✅ Signals sent successfully via SignalR."
                    : "⚠️ SignalR failed to send signals.");
            }
        }
        catch (Exception ex)
        {
            await _logger.LogError("❌ Exception in StartSignalGeneration", ex);
        }
    }

    public async Task<List<SignalData>> GetSignalsAsync(SignalData config, IProtocolCommunication protocolCommunication)
    {
        await _logger.LogInfo($"🔍 GetSignalsAsync: Fetching signals for config '{config.Name}'");

        try
        {
            var signals = await protocolCommunication.ReceiveSignalsAsync(config);
            await _logger.LogInfo($"📥 Retrieved {signals.Count} signals.");

            var signalR = GetSignalRProtocol();
            if (signalR != null)
            {
                bool result = await signalR.SendSignalsAsync(signals);
                await _logger.LogInfo(result
                    ? "✅ Signals forwarded via SignalR."
                    : "⚠️ Failed to send signals via SignalR.");
            }

            return signals;
        }
        catch (Exception ex)
        {
            await _logger.LogError("❌ Exception in GetSignalsAsync", ex);
            return new List<SignalData>();
        }
    }

    public async Task<bool> SendSignalsAsync(List<SignalData> signalData, IProtocolCommunication protocolCommunication)
    {
        await _logger.LogInfo($"📤 SendSignalsAsync: Sending {signalData.Count} signals.");

        try
        {
            bool result = await protocolCommunication.SendSignalsAsync(signalData);
            await _logger.LogInfo(result
                ? "✅ Signals sent successfully."
                : "⚠️ Failed to send signals.");

            return result;
        }
        catch (Exception ex)
        {
            await _logger.LogError("❌ Exception in SendSignalsAsync", ex);
            return false;
        }
    }

    public async Task<bool> MonitorSignalStatus(IProtocolCommunication protocolCommunication)
    {
        await _logger.LogInfo("📡 Monitoring signal status...");

        try
        {
            bool isOnline = await protocolCommunication.MonitorStatusAsync();
            await _logger.LogInfo(isOnline
                ? "✅ Signal is online."
                : "⚠️ Signal is offline.");

            return isOnline;
        }
        catch (Exception ex)
        {
            await _logger.LogError("❌ Exception in MonitorSignalStatus", ex);
            return false;
        }
    }
}
