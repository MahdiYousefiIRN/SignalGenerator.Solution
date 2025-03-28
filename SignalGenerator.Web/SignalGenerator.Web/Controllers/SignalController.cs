using Microsoft.AspNetCore.Mvc;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Services;
using SignalGenerator.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.SignalR;

namespace SignalGenerator.Web.Controllers
{
    public class SignalController : Controller
    {
        private readonly SignalProcessorService _signalProcessorService;

        public SignalController(SignalProcessorService signalProcessorService)
        {
            _signalProcessorService = signalProcessorService;
        }

        // متد برای نمایش صفحه Index
        public IActionResult Index()
        {
            return View(); // اینجا View به دنبال /Views/Signal/Index.cshtml می‌گردد
        }

        // متد برای دریافت سیگنال‌ها از پروتکل
        [HttpGet("get")]
        public async Task<IActionResult> GetSignals(int count, string protocolType)
        {
            IProtocolCommunication protocolCommunication = GetProtocol(protocolType);

            var config = new SignalConfig { SignalCount = count };
            var signals = await _signalProcessorService.GetSignalsAsync(config, protocolCommunication);

            if (signals == null || !signals.Any())
                return NotFound("No signals found.");

            return Ok(signals);
        }

        // متد برای ارسال سیگنال‌ها
        [HttpPost("post")]
        public async Task<IActionResult> PostSignals([FromBody] List<SignalData> signalData, string protocolType)
        {
            IProtocolCommunication protocolCommunication = GetProtocol(protocolType);

            var result = await _signalProcessorService.SendSignalsAsync(signalData, protocolCommunication);

            if (result)
                return Ok("Signals sent successfully.");
            return BadRequest("Failed to send signals.");
        }

        // متد برای نظارت بر وضعیت سیگنال‌ها
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(string protocolType)
        {
            IProtocolCommunication protocolCommunication = GetProtocol(protocolType);

            var status = await _signalProcessorService.MonitorSignalStatus(protocolCommunication);

            if (status)
                return Ok("Status is active.");
            return BadRequest("Status monitoring failed.");
        }

        // متد برای شناسایی نوع پروتکل و بازگشت پروتکل مناسب
        private IProtocolCommunication GetProtocol(string protocolType)
        {
            switch (protocolType.ToLower())
            {
                case "http":
                    return new Http_Protocol("http://localhost:5000");
                case "modbus":
                    return new ModbusProtocol("192.168.1.100", 502);
                case "signalar":
                    return new SignalRProtocol("http://localhost:5000/signalhub");
                default:
                    throw new ArgumentException("Invalid protocol type.");
            }
        }
    }
}
