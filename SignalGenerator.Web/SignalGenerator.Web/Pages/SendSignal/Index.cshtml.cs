using Microsoft.AspNetCore.Mvc;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Services;
using SignalGenerator.Data.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.SignalR;
using SignalGenerator.Protocols.Modbus;
using Microsoft.Extensions.Logging;

namespace SignalGenerator.Web.Pages.SendSignal
{
    public class IndexModel : PageModel
    {
        private readonly SignalProcessorService _signalProcessorService;
        private readonly ILogger<IndexModel> _logger;
        private static readonly Random _random = new Random();

        public IndexModel(SignalProcessorService signalProcessorService, ILogger<IndexModel> logger)
        {
            _signalProcessorService = signalProcessorService;
            _logger = logger;
        }

        [BindProperty]
        public string ProtocolType { get; set; }
        [BindProperty]
        public int SignalCount { get; set; }
        [BindProperty]
        public int Duration { get; set; }

        public string SendResult { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var protocolType = ProtocolType;
                var signalCount = SignalCount;
                var duration = Duration;

                var signals = GenerateRandomSignals(signalCount);

                IProtocolCommunication protocolCommunication = GetProtocol(protocolType);

                var result = await _signalProcessorService.SendSignalsAsync(signals, protocolCommunication);

                return new JsonResult(new { sendResult = result ? "Signals sent successfully." : "Failed to send signals." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending signals.");
                return new JsonResult(new { sendResult = $"Error: {ex.Message}" });
            }
        }

        private List<SignalData> GenerateRandomSignals(int count)
        {
            var signals = new List<SignalData>();

            for (int i = 0; i < count; i++)
            {
                double randomFrequency = _random.NextDouble() * (70 - 40) + 40;
                double randomPower = _random.NextDouble() * 100;
                DateTime timestamp = DateTime.UtcNow;

                var signal = new SignalData
                {
                    Frequency = randomFrequency,
                    Power = randomPower,
                    Timestamp = timestamp,
                    CoilStatus = _random.NextDouble() > 0.5,
                    DiscreteInputStatus = _random.NextDouble() > 0.5,
                    ProtocolType = ProtocolType // Set the required ProtocolType property
                };

                signals.Add(signal);
            }

            return signals;
        }

        private IProtocolCommunication GetProtocol(string protocolType)
        {
            var protocols = new Dictionary<string, Func<IProtocolCommunication>>(StringComparer.OrdinalIgnoreCase)
    {
        { "http", () => new Http_Protocol("http://localhost:5000", _logger as ILogger<Http_Protocol>) },
        { "modbus", () => new ModbusProtocol("192.168.1.100", 502, _logger as ILogger<ModbusProtocol>) },
        { "signalar", () => new SignalRProtocol("http://localhost:5000/signalhub", _logger as ILogger<SignalRProtocol>) }
    };

            if (protocols.TryGetValue(protocolType, out var createProtocol))
            {
                return createProtocol();
            }

            throw new ArgumentException($"Invalid protocol type: {protocolType}");
        }

    }
}
