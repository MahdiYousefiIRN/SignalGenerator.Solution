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
    {
        return _protocols.OfType<SignalRProtocol>().FirstOrDefault();
    }

    public async Task StartSignalGeneration(SignalData config, IProtocolCommunication protocolCommunication)
    {
        try
        {
            await _logger.LogInfo($"🛠️ Starting signal generation for {config.Name}...");

            var signalData = await protocolCommunication.ReceiveSignalsAsync(config);
            bool sendSuccess = await protocolCommunication.SendSignalsAsync(signalData);

            if (!sendSuccess)
            {
                await _logger.LogError("❌ Failed to send signals via protocol.");
            }

            var signalR = GetSignalRProtocol();
            if (signalR != null)
            {
                bool signalRSent = await signalR.SendSignalsAsync(signalData);
                if (!signalRSent)
                    await _logger.LogError("❌ Failed to send signals via SignalR.");
                else
                    await _logger.LogInfo("✅ Signals sent successfully via SignalR.");
            }
        }
        catch (Exception ex)
        {
            await _logger.LogError($"❌ Error in StartSignalGeneration: {ex.Message}", ex);
        }
    }

    public async Task<List<SignalData>> GetSignalsAsync(SignalData config, IProtocolCommunication protocolCommunication)
    {
        try
        {
            await _logger.LogInfo("🔄 Retrieving signals...");

            var signals = await protocolCommunication.ReceiveSignalsAsync(config);

            var signalR = GetSignalRProtocol();
            if (signalR != null)
            {
                bool signalRSent = await signalR.SendSignalsAsync(signals);
                if (!signalRSent)
                    await _logger.LogError("❌ Failed to send signals via SignalR.");
                else
                    await _logger.LogInfo("✅ Signals sent successfully via SignalR.");
            }

            return signals;
        }
        catch (Exception ex)
        {
            await _logger.LogError($"❌ Error in GetSignalsAsync: {ex.Message}", ex);
            return new List<SignalData>();
        }
    }

    public async Task<bool> SendSignalsAsync(List<SignalData> signalData, IProtocolCommunication protocolCommunication)
    {
        try
        {
            await _logger.LogInfo($"📡 Sending {signalData.Count} signals...");

            bool result = await protocolCommunication.SendSignalsAsync(signalData);

            if (!result)
            {
                await _logger.LogError("❌ Failed to send signals via protocol.");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _logger.LogError($"❌ Error in SendSignalsAsync: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> MonitorSignalStatus(IProtocolCommunication protocolCommunication)
    {
        try
        {
            await _logger.LogInfo("📡 Monitoring signal status...");

            bool result = await protocolCommunication.MonitorStatusAsync();

            if (!result)
            {
                await _logger.LogError("❌ Signal status is offline.");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _logger.LogError($"❌ Error in MonitorSignalStatus: {ex.Message}", ex);
            return false;
        }
    }
}
