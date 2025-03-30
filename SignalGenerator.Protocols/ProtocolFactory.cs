using SignalGenerator.Data.Interfaces;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SignalGenerator.Protocols
{
    public static class ProtocolFactory
    {
        public static IServiceCollection AddProtocolServices(this IServiceCollection services)
        {
            services.AddTransient<Http_Protocol>();
            services.AddTransient<ModbusProtocol>();
            return services;
        }

        public static IProtocolCommunication CreateProtocol(
            IServiceProvider serviceProvider,
            string protocolType,
            string ip = "",
            int port = 0)
        {
            switch (protocolType.ToLower())
            {
                case "modbus":
                    var modbusLogger = serviceProvider.GetRequiredService<ILogger<ModbusProtocol>>();
                    return new ModbusProtocol(ip, port, modbusLogger);
                
                case "http":
                    var httpLogger = serviceProvider.GetRequiredService<ILogger<Http_Protocol>>();
                    return new Http_Protocol($"http://{ip}:{port}", httpLogger);
                
                default:
                    throw new ArgumentException($"Unsupported protocol type: {protocolType}");
            }
        }

        public static IProtocolCommunication CreateProtocol(
            string protocolType,
            string ip = "",
            int port = 0)
        {
            // This method is kept for backward compatibility
            // It creates a new ServiceProvider instance for each call
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
            services.AddProtocolServices();
            
            using var serviceProvider = services.BuildServiceProvider();
            return CreateProtocol(serviceProvider, protocolType, ip, port);
        }
    }
}
