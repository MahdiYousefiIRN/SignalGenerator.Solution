using Microsoft.Extensions.DependencyInjection;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.SignalProtocol;
using SignalGenerator.Helpers;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;

public class ProtocolFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProtocolConfigProvider _configProvider;

    public ProtocolFactory(IServiceProvider serviceProvider, ProtocolConfigProvider configProvider)
    {
        _serviceProvider = serviceProvider;
        _configProvider = configProvider;
    }

    public IProtocolCommunication CreateProtocol(string protocolType, string ipAddress, int port)
    {
        // دریافت سرویس لاگ‌گیری
        var logger = _serviceProvider.GetRequiredService<ILoggerService>();

        // دریافت پیکربندی پروتکل
        var config = _configProvider.GetConfig(protocolType);

        // انتخاب و ایجاد پروتکل مناسب
        switch (protocolType.ToLowerInvariant())  // استفاده از ToLowerInvariant برای ثبات بیشتر
        {
            case "modbus":
                var modbusConfig = (ModbusConfig)config;
                return new ModbusProtocol(modbusConfig.IpAddress, modbusConfig.Port, logger);

            case "http":
                var httpConfig = (HttpConfig)config;
                var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                var baseUrl = $"http://{httpConfig.IpAddress}:{httpConfig.Port}{httpConfig.BasePath}";
                return new Http_Protocol(httpClientFactory, baseUrl, logger);

            case "signalar":
                var signalrConfig = (SignalRConfig)config;
                var hubUrl = $"http://{signalrConfig.IpAddress}:{signalrConfig.Port}{signalrConfig.HubUrl}";
                return new SignalRProtocol(hubUrl, logger);

            default:
                // در صورت پشتیبانی نکردن پروتکل
                logger.LogError($"Unsupported protocol type: {protocolType}");
                throw new NotSupportedException($"Unsupported protocol: {protocolType}");
        }
    }
}
