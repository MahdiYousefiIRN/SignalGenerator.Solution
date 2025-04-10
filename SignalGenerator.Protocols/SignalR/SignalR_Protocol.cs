using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SignalGenerator.Protocols.SignalR
{
    public class SignalR_Protocol : IProtocolCommunication, IDisposable
    {
        private readonly string _hubUrl;
        private readonly ILoggerService _logger;
        private HubConnection _hubConnection;

        public SignalR_Protocol(string hubUrl, ILoggerService logger)
        {
            _hubUrl = hubUrl ?? throw new ArgumentNullException(nameof(hubUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeHubConnection();
        }

        private void InitializeHubConnection()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .Build();

            _hubConnection.On<string>("ReceiveMessage", message =>
            {
                _logger.LogInfo($"Received message from SignalR Hub: {message}");
            });

            _hubConnection.Closed += async (exception) =>
            {
                _logger.LogWarning($"Connection closed. Attempting to reconnect...");
                await ReconnectAsync();
            };
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || signalData.Count == 0)
            {
                _logger.LogWarning("No signal data provided.");
                return false;
            }

            try
            {
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await ReconnectAsync();
                }

                await _hubConnection.SendAsync("SendSignals", signalData);
                _logger.LogInfo($"Successfully sent {signalData.Count} signals to SignalR Hub.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending signals to SignalR Hub: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            try
            {
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await ReconnectAsync();
                }

                var signals = await _hubConnection.InvokeAsync<List<SignalData>>("ReceiveSignals", config);
                if (signals != null && signals.Count > 0)
                {
                    _logger.LogInfo($"Received {signals.Count} signals from SignalR Hub.");
                    return signals;
                }

                _logger.LogWarning("No signals received from SignalR Hub.");
                return new List<SignalData>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error receiving signals from SignalR Hub: {ex.Message}", ex);
                return new List<SignalData>();
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await ReconnectAsync();
                }

                var status = await _hubConnection.InvokeAsync<bool>("CheckStatus");
                _logger.LogInfo($"SignalR Hub status: {status}");
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking SignalR status: {ex.Message}", ex);
                return false;
            }
        }

        private async Task ReconnectAsync()
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInfo("Attempting to reconnect to SignalR Hub...");
                    await _hubConnection.StartAsync();
                    _logger.LogInfo($"Reconnected to SignalR Hub at {_hubUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reconnecting to SignalR Hub: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async Task DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

}
