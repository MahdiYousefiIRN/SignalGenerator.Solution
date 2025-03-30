using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Services;
using SignalGenerator.Protocols;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SignalGenerator.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SignalController : ControllerBase
    {
        private readonly IProtocolDataStore _dataStore;
        private readonly ILogger<SignalController> _logger;
        private readonly SignalProcessorService _signalProcessor;

        public SignalController(IProtocolDataStore dataStore, ILogger<SignalController> logger, SignalProcessorService signalProcessor)
        {
            _dataStore = dataStore;
            _logger = logger;
            _signalProcessor = signalProcessor;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSignals([FromBody] SignalConfig config)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                config.UserId = userId;
                config.CreatedAt = DateTime.UtcNow;

                var signals = await _signalProcessor.GetSignalsAsync(config);
                await _dataStore.SaveSignalsAsync(signals);

                return Ok(new { success = true, message = "Signals generated successfully", signals });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signals");
                return StatusCode(500, new { success = false, message = "Error generating signals" });
            }
        }

        [HttpGet("signals")]
        public async Task<IActionResult> GetSignals([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var signals = await _dataStore.GetSignalsAsync(userId, startTime, endTime);
                return Ok(signals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving signals");
                return StatusCode(500, new { success = false, message = "Error retrieving signals" });
            }
        }
    }
}



namespace SignalGenerator.Data.Services
{
    public class SignalProcessorService
    {
        private readonly IProtocolCommunication _protocolCommunication;
        private readonly ILogger<SignalProcessorService> _logger;

        public SignalProcessorService(IProtocolCommunication protocolCommunication, ILogger<SignalProcessorService> logger)
        {
            _protocolCommunication = protocolCommunication;
            _logger = logger;
        }

        public async Task<List<SignalData>> GetSignalsAsync(SignalConfig config)
        {
            try
            {
                // دریافت سیگنال‌ها از پروتکل
                var signals = await _protocolCommunication.ReceiveSignalsAsync(config);
                // ارسال سیگنال‌ها به سرور یا دستگاه مقصد
                await _protocolCommunication.SendSignalsAsync(signals);
                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signals");
                throw;
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signals, IProtocolCommunication protocolCommunication)
        {
            try
            {
                await protocolCommunication.SendSignalsAsync(signals);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signals");
                return false;
            }
        }
    }

}
