using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SignalGenerator.Core.Models;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;

namespace SignalGenerator.Protocols.SignalR
{
    public class SignalRProtocol : IProtocolCommunication, IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRProtocol> _logger;

        public SignalRProtocol(string hubUrl, ILogger<SignalRProtocol> logger)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentNullException(nameof(hubUrl), "Hub URL cannot be null or empty.");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    _logger.LogInformation("Connecting to SignalR hub...");
                    await _connection.StartAsync();
                    _logger.LogInformation("Connected to SignalR hub.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to connect to SignalR hub: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            try
            {
                await EnsureConnectionAsync();
                await _connection.SendAsync("SendSignals", signalData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendSignalsAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            var signalsTcs = new TaskCompletionSource<List<SignalData>>();

            try
            {
                await EnsureConnectionAsync();

                _connection.On<List<SignalData>>("ReceiveSignals", (receivedSignals) =>
                {
                    signalsTcs.TrySetResult(receivedSignals);
                });

                await _connection.SendAsync("RequestSignals", config.SignalCount);

                return await signalsTcs.Task; // منتظر دریافت داده‌ها می‌ماند
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ReceiveSignalsAsync: {ex.Message}");
                return new List<SignalData>();
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                await EnsureConnectionAsync();
                var status = await _connection.InvokeAsync<bool>("MonitorStatus");
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MonitorStatusAsync: {ex.Message}");
                return false;
            }
        }

        // ✅ متد DisposeAsync برای آزادسازی منابع
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                _logger.LogInformation("Disposing SignalR connection...");
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _logger.LogInformation("SignalR connection disposed.");
            }
        }
    }
}
