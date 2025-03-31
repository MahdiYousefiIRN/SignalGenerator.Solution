using SignalGenerator.Core.Models;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Protocols.SignalR; // اضافه کردن استفاده از SignalR
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SignalGenerator.Data.Services
{
    public class SignalProcessorService
    {
        private readonly SignalRProtocol _signalRProtocol;
        private readonly ILogger<SignalProcessorService> _logger;

        // Dependency Injection برای SignalRProtocol و Logger
        public SignalProcessorService(SignalRProtocol signalRProtocol, ILogger<SignalProcessorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // بررسی مقدار null

            _signalRProtocol = signalRProtocol;
        }

        /// <summary>
        /// Starts the generation of signals using the specified configuration and protocol communication.
        /// </summary>
        /// <param name="config">The configuration for signal generation.</param>
        /// <param name="protocolCommunication">The protocol communication interface to use.</param>
        public async Task StartSignalGeneration(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            try
            {
                // دریافت سیگنال‌ها از پروتکل
                var signalData = await protocolCommunication.ReceiveSignalsAsync(config);

                // ارسال سیگنال‌ها به پروتکل
                bool sendSuccess = await protocolCommunication.SendSignalsAsync(signalData);
                if (!sendSuccess)
                {
                    _logger.LogWarning("Failed to send signals via protocol.");
                }

                // ارسال سیگنال‌ها به SignalR
                bool signalRSent = await _signalRProtocol.SendSignalsAsync(signalData);
                if (!signalRSent)
                {
                    _logger.LogWarning("Failed to send signals via SignalR.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in StartSignalGeneration: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves signals based on the configuration and protocol communication.
        /// </summary>
        /// <param name="config">The configuration for retrieving signals.</param>
        /// <param name="protocolCommunication">The protocol communication interface to use.</param>
        /// <returns>A list of SignalData objects.</returns>
        public async Task<List<SignalData>> GetSignalsAsync(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            try
            {
                // دریافت سیگنال‌ها از پروتکل
                var signals = await protocolCommunication.ReceiveSignalsAsync(config);

                // ارسال سیگنال‌ها به SignalR
                bool signalRSent = await _signalRProtocol.SendSignalsAsync(signals);
                if (!signalRSent)
                {
                    _logger.LogWarning("Failed to send signals via SignalR.");
                }

                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSignalsAsync: {ex.Message}");
                return new List<SignalData>();
            }
        }

        /// <summary>
        /// Sends a list of signals using the protocol communication.
        /// </summary>
        /// <param name="signalData">The list of signals to send.</param>
        /// <param name="protocolCommunication">The protocol communication interface to use.</param>
        /// <returns>A boolean indicating whether the signals were sent successfully.</returns>
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData, IProtocolCommunication protocolCommunication)
        {
            try
            {
               
                // ارسال سیگنال‌ها به پروتکل
                bool result = await protocolCommunication.SendSignalsAsync(signalData);
                if (!result)
                {
                    _logger.LogWarning("Failed to send signals via protocol.");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendSignalsAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Monitors the status of signals using the protocol communication.
        /// </summary>
        /// <param name="protocolCommunication">The protocol communication interface to use.</param>
        /// <returns>A boolean indicating whether the signal status is being monitored successfully.</returns>
        public async Task<bool> MonitorSignalStatus(IProtocolCommunication protocolCommunication)
        {
            try
            {
                // نظارت بر وضعیت از طریق پروتکل
                bool result = await protocolCommunication.MonitorStatusAsync();
                if (!result)
                {
                    _logger.LogWarning("Failed to monitor signal status.");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MonitorSignalStatus: {ex.Message}");
                return false;
            }
        }
    }
}
