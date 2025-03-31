using EasyModbus;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Protocols.Modbus
{
    public class ModbusProtocol : IProtocolCommunication
    {
        private readonly ModbusClient _modbusClient;
        private readonly ILogger<ModbusProtocol> _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private readonly object _operationLock = new object();
        private const int MaxRetries = 3;
        private const int ConnectionTimeoutMs = 5000;
        private const int OperationTimeoutMs = 3000;

        public ModbusProtocol(string ipAddress, int port, ILogger<ModbusProtocol> logger)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP address cannot be null or empty.");

            if (port <= 0)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than zero.");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _modbusClient = new ModbusClient(ipAddress, port)
            {
                ConnectionTimeout = ConnectionTimeoutMs
            };
        }


        private async Task EnsureConnectionAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!_modbusClient.Connected)
                {
                    _logger.LogInformation("Connecting to Modbus device at {IpAddress}:{Port}", 
                        _modbusClient.IPAddress, _modbusClient.Port);
                    
                    for (int i = 0; i < MaxRetries; i++)
                    {
                        try
                        {
                            _modbusClient.Connect();
                            _logger.LogInformation("Successfully connected to Modbus device");
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Connection attempt {Attempt} failed", i + 1);
                            if (i < MaxRetries - 1)
                            {
                                await Task.Delay(1000 * (i + 1));
                            }
                        }
                    }
                    throw new ProtocolException("Failed to connect to Modbus device after multiple attempts");
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config)
        {
            try
            {
                await EnsureConnectionAsync();
                var signals = new List<SignalData>();
                
                lock (_operationLock)
                {
                    var registers = _modbusClient.ReadHoldingRegisters(0, config.SignalCount);
                    for (int i = 0; i < config.SignalCount; i++)
                    {
                        signals.Add(new SignalData
                        {
                            Frequency = registers[i] / 10.0,
                            Power = registers[i] * 2,
                            Timestamp = DateTime.UtcNow,
                            ProtocolType = "Modbus"
                        });
                    }
                }

                _logger.LogInformation("Successfully received {Count} signals from Modbus device", signals.Count);
                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving signals from Modbus device");
                throw new ProtocolException("Failed to receive signals from Modbus device", ex);
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || !signalData.Any())
            {
                _logger.LogWarning("Attempted to send empty signal data to Modbus device");
                return false;
            }

            try
            {
                await EnsureConnectionAsync();
                var values = signalData.Select(s => (int)(s.Frequency * 10)).ToArray();
                
                lock (_operationLock)
                {
                    _modbusClient.WriteMultipleRegisters(0, values);
                }

                _logger.LogInformation("Successfully sent {Count} signals to Modbus device", signalData.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signals to Modbus device");
                throw new ProtocolException("Failed to send signals to Modbus device", ex);
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                await EnsureConnectionAsync();
                lock (_operationLock)
                {
                    var testRegister = _modbusClient.ReadHoldingRegisters(0, 1);
                    var status = testRegister.Length > 0;
                    _logger.LogInformation("Modbus device status check result: {Status}", status);
                    return status;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Modbus device status");
                throw new ProtocolException("Failed to monitor Modbus device status", ex);
            }
        }

        public void Dispose()
        {
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
                _logger.LogError(ex, "Error disposing Modbus protocol");
            }
        }
    }
    public class HttpProtocolOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message)
        {
        }

        public ProtocolException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
