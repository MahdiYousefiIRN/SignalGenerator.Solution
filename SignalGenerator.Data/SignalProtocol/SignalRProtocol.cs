using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Helpers;

namespace SignalGenerator.Data.SignalProtocol
{
    public class SignalRProtocol : IProtocolCommunication, IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly ILoggerService _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _isConnecting = false;
        private bool _disposed = false;

        private const int MaxRetries = 3;
        private const int ReconnectDelayMs = 5000;

        public SignalRProtocol(string hubUrl, ILoggerService logger)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentNullException(nameof(hubUrl), "❌ Hub URL cannot be null or empty.");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += (exception) =>
            {
                _logger.LogAsync("⚠️ Attempting to reconnect to SignalR hub...", LogLevel.Warning);
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                _logger.LogAsync($"✅ Successfully reconnected to SignalR hub. Connection ID: {connectionId}", LogLevel.Info);
                return Task.CompletedTask;
            };

            _connection.Closed += async (exception) =>
            {
                _ = _logger.LogAsync("❌ SignalR connection closed. Retrying...", LogLevel.Error);
                await Task.Delay(ReconnectDelayMs);
                await EnsureConnectionAsync();
            };
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection.State == HubConnectionState.Connected || _isConnecting) return;

            await _connectionLock.WaitAsync();
            try
            {
                if (_connection.State == HubConnectionState.Connected) return;

                _isConnecting = true;
                await _logger.LogAsync("🔄 Attempting to connect to SignalR hub...", LogLevel.Info);

                for (int i = 0; i < MaxRetries; i++)
                {
                    try
                    {
                        await _connection.StartAsync();
                        await _logger.LogAsync("✅ Successfully connected to SignalR hub.", LogLevel.Info);
                        return;
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogAsync($"⚠ Connection attempt {i + 1} failed: {ex.Message}", LogLevel.Warning);
                        if (i < MaxRetries - 1) await Task.Delay(ReconnectDelayMs * (i + 1));
                    }
                }

                throw new ProtocolException("❌ Failed to connect to SignalR hub after multiple attempts");
            }
            finally
            {
                _isConnecting = false;
                _connectionLock.Release();
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || signalData.Count == 0)
            {
                await _logger.LogAsync("⚠ Attempted to send empty signal data to SignalR hub.", LogLevel.Warning);
                return false;
            }

            await EnsureConnectionAsync();

            try
            {
                await _logger.LogAsync($"📡 Sending {signalData.Count} signals...", LogLevel.Info);
                await _connection.SendAsync("SendSignals", signalData);
                await _logger.LogAsync("✅ Signals sent successfully.", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"❌ Error sending signals to SignalR hub: {ex.Message}", LogLevel.Error, ex);
                return false;
            }
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");

            await EnsureConnectionAsync();

            var signalsTcs = new TaskCompletionSource<List<SignalData>>();

            try
            {
                await _logger.LogAsync("📡 Listening for incoming signals...", LogLevel.Info);

                _connection.On<List<SignalData>>("ReceiveSignals", (receivedSignals) =>
                {
                    _logger.LogAsync($"✅ Received {receivedSignals?.Count ?? 0} signals.", LogLevel.Info);
                    signalsTcs.TrySetResult(receivedSignals);
                });

                await _connection.SendAsync("RequestSignals", config.SignalCount);
                await _logger.LogAsync("🔄 Requested signals from server...", LogLevel.Info);

                return await signalsTcs.Task;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"❌ Error receiving signals from SignalR hub: {ex.Message}", LogLevel.Error, ex);
                return new List<SignalData>();
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            await EnsureConnectionAsync();

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await _logger.LogAsync("📡 Checking server status...", LogLevel.Info);
                    var status = await _connection.InvokeAsync<bool>("MonitorStatus");

                    await _logger.LogAsync($"✅ Server status: {(status ? "🟢 Online" : "🔴 Offline")}", status ? LogLevel.Info : LogLevel.Warning);
                    return status;
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync($"❌ Error checking SignalR server status (Attempt {attempt}/{MaxRetries}): {ex.Message}", LogLevel.Warning, ex);
                    if (attempt == MaxRetries)
                        throw new ProtocolException("Failed to monitor SignalR server status after multiple attempts", ex);
                    await Task.Delay(ReconnectDelayMs * attempt);
                }
            }
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            try
            {
                await _logger.LogAsync("🛑 Disposing SignalR connection...", LogLevel.Warning);
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    await _connection.StopAsync();
                }
                await _connection.DisposeAsync();
                await _logger.LogAsync("✅ SignalR connection disposed.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"⚠ Error disposing SignalR connection: {ex.Message}", LogLevel.Error, ex);
            }
            finally
            {
                _connectionLock.Dispose();
                _disposed = true;
            }
        }
    }

    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message) { }
        public ProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
}
