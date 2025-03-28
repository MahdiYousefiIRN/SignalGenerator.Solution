using EasyModbus;
using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Protocols.Modbus
{
    public class ModbusProtocol : IProtocolCommunication
    {
        private readonly ModbusClient _modbusClient;
        private readonly object _lock = new object();

        public ModbusProtocol(string ipAddress, int port)
        {
            _modbusClient = new ModbusClient(ipAddress, port);
        }

        private void EnsureConnection()
        {
            if (!_modbusClient.Connected)
            {
                _modbusClient.Connect();
            }
        }

        public Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config)
        {
            EnsureConnection();
            var signals = new List<SignalData>();
            lock (_lock)
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
            return Task.FromResult(signals);
        }

        public Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            EnsureConnection();
            var values = signalData.Select(s => (int)(s.Frequency * 10)).ToArray();
            lock (_lock)
            {
                _modbusClient.WriteMultipleRegisters(0, values);
            }
            return Task.FromResult(true);
        }

        public Task<bool> MonitorStatusAsync()
        {
            EnsureConnection();
            lock (_lock)
            {
                try
                {
                    var testRegister = _modbusClient.ReadHoldingRegisters(0, 1);
                    return Task.FromResult(testRegister.Length > 0);
                }
                catch
                {
                    return Task.FromResult(false);
                }
            }
        }
    }
}
