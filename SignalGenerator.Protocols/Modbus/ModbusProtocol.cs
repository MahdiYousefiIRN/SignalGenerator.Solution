using EasyModbus;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalGenerator.Protocols.Modbus
{
    public class ModbusProtocol : IProtocolCommunication, IDisposable
    {
        private readonly ModbusClient _modbusClient;
        private readonly ILoggerService _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;
        private bool _isConnecting = false;

        private const int MaxRetries = 3;
        private const int ConnectionTimeoutMs = 5000;
        private const int OperationTimeoutMs = 3000;

        public ModbusProtocol(string ipAddress, int port, ILoggerService logger)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP address cannot be null or empty.");
            if (port <= 0)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than zero.");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modbusClient = new ModbusClient(ipAddress, port) { ConnectionTimeout = ConnectionTimeoutMs };
        }

        private async Task LogAsync(string message, LogLevel level = LogLevel.Info, Exception? exception = null) =>
            await _logger.LogAsync(message, level, exception);

        private async Task EnsureConnectionAsync()
        {
            if (_modbusClient.Connected || _isConnecting) return;

            await _connectionLock.WaitAsync();
            try
            {
                if (_modbusClient.Connected) return;

                _isConnecting = true;
                await LogAsync($"🔄 Attempting to connect to Modbus device at {_modbusClient.IPAddress}:{_modbusClient.Port}...", LogLevel.Info);

                for (int i = 0; i < MaxRetries; i++)
                {
                    try
                    {
                        _modbusClient.Connect();
                        await LogAsync("✅ Successfully connected to Modbus device", LogLevel.Info);
                        return;
                    }
                    catch (Exception ex)
                    {
                        await LogAsync($"⚠ Connection attempt {i + 1} failed: {ex.Message}", LogLevel.Warning);
                        if (i < MaxRetries - 1) await Task.Delay(1000 * (i + 1));
                    }
                }

                throw new ProtocolException("❌ Failed to connect to Modbus device after multiple attempts");
            }
            finally
            {
                _isConnecting = false;
                _connectionLock.Release();
            }
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");

            await EnsureConnectionAsync();

            try
            {
                var registers = await Task.Run(() => _modbusClient.ReadHoldingRegisters(0, config.SignalCount));

                var signals = registers.Select((value, index) => new SignalData
                {
                    Frequency = value / 10.0,
                    Power = value * 2,
                    Timestamp = DateTime.UtcNow,
                    ProtocolType = "Modbus"
                }).ToList();

                await LogAsync($"📥 Received {signals.Count} signals from Modbus device", LogLevel.Info);
                return signals;
            }
            catch (Exception ex)
            {
                await LogAsync("❌ Error receiving signals from Modbus device", LogLevel.Error, ex);
                throw new ProtocolException("Failed to receive signals from Modbus device", ex);
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || !signalData.Any())
            {
                await LogAsync("⚠ Attempted to send empty signal data to Modbus device", LogLevel.Warning);
                return false;
            }

            await EnsureConnectionAsync();

            try
            {
                var values = signalData.Select(s => (int)(s.Frequency * 10)).ToArray();

                await Task.Run(() => _modbusClient.WriteMultipleRegisters(0, values));

                await LogAsync($"📤 Sent {signalData.Count} signals to Modbus device", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                await LogAsync("❌ Error sending signals to Modbus device", LogLevel.Error, ex);
                throw new ProtocolException("Failed to send signals to Modbus device", ex);
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            await EnsureConnectionAsync();

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var cts = new CancellationTokenSource(OperationTimeoutMs);
                    bool status = await Task.Run(() =>
                    {
                        var testRegister = _modbusClient.ReadHoldingRegisters(0, 1);
                        return testRegister.Length > 0;
                    }, cts.Token);

                    await LogAsync($"📡 Modbus device status check result: {status}", LogLevel.Info);
                    return status;
                }
                catch (Exception ex)
                {
                    await LogAsync($"❌ Error checking Modbus device status (Attempt {attempt}/{MaxRetries})", LogLevel.Warning, ex);
                    if (attempt == MaxRetries)
                        throw new ProtocolException("Failed to monitor Modbus device status after multiple attempts", ex);
                    await Task.Delay(500 * attempt);
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_modbusClient?.Connected == true)
                {
                    _modbusClient.Disconnect();
                }
                _connectionLock?.Dispose();
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync("⚠ Error disposing Modbus protocol", LogLevel.Error, ex);
            }
            finally
            {
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
