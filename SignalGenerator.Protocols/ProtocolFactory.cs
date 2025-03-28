using SignalGenerator.Core.Interfaces;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;

namespace SignalGenerator.Protocols
{
    public static class ProtocolFactory
    {
        public static IProtocolCommunication CreateProtocol(string protocolType, string ip = "", int port = 0)
        {
            switch (protocolType.ToLower())
            {
                case "modbus":
                    return new ModbusProtocol(ip, port);
                case "http":
                    return new Http_Protocol($"http://{ip}:{port}");
                // در اینجا می‌توانید پروتکل‌های جدید مانند IEC101, IEC104 و غیره را اضافه کنید.
                default:
                    throw new ArgumentException("Invalid protocol type");
            }
        }
    }
}
