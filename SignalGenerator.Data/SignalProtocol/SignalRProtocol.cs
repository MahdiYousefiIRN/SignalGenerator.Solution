using Microsoft.AspNetCore.SignalR;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Protocols.SignalR
{
    public class SignalRProtocol : IProtocolCommunication
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRProtocol> _logger;

        // سازنده برای ایجاد ارتباط SignalR
        public SignalRProtocol(string hubUrl, ILogger<SignalRProtocol> logger)
        {
            _logger = logger;
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();
        }

        // متد برای دریافت سیگنال‌ها از SignalR
        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config)
        {
            var signals = new List<SignalData>();

            try
            {
                await _connection.StartAsync();

                await _connection.SendAsync("RequestSignals", config.SignalCount);

                _connection.On<List<SignalData>>("ReceiveSignals", (receivedSignals) =>
                {
                    signals = receivedSignals;
                });

                await Task.Delay(5000); // منتظر 5 ثانیه

                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ReceiveSignalsAsync: {ex.Message}");
                return new List<SignalData>();
            }
        }

        // متد برای ارسال سیگنال‌ها به SignalR
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            try
            {
                await _connection.StartAsync();

                await _connection.SendAsync("SendSignals", signalData);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendSignalsAsync: {ex.Message}");
                return false;
            }
        }

        // متد برای نظارت بر وضعیت سیگنال‌ها
        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                await _connection.StartAsync();

                var status = await _connection.InvokeAsync<bool>("MonitorStatus");

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MonitorStatusAsync: {ex.Message}");
                return false;
            }
        }
    }
}
