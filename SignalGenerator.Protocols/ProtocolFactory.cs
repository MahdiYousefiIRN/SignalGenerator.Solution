using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.SignalProtocol;
using SignalGenerator.Helpers;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Protocols.SignalR;

namespace SignalGenerator.Protocols
{
    public class ProtocolFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ProtocolFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // متدی برای دریافت پروتکل بر اساس نوع پروتکل
        public IProtocolCommunication GetProtocol(string protocolType)
        {
            // استفاده از تنظیمات پیکربندی برای آدرس IP و پورت پیش‌فرض
            string defaultIp = _configuration.GetValue<string>("ProtocolSettings:DefaultIp", "127.0.0.1");
            int defaultPort = _configuration.GetValue<int>("ProtocolSettings:DefaultPort", 5000);

            // ساخت پروتکل با آدرس پیش‌فرض و پورت پیش‌فرض
            return CreateProtocol(protocolType, defaultIp, defaultPort);
        }

        // متدی برای ایجاد پروتکل‌ها بر اساس نوع پروتکل و اطلاعات IP و پورت
        public IProtocolCommunication CreateProtocol(string protocolType, string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(protocolType))
                throw new ArgumentException("Protocol type cannot be null or empty.", nameof(protocolType));

            if (string.IsNullOrWhiteSpace(ip) || port <= 0)
                throw new ArgumentException("IP and port must be provided and valid.");

            var loggerService = _serviceProvider.GetRequiredService<ILoggerService>();

            loggerService.LogInfo($"Creating protocol: {protocolType} for IP: {ip} and port: {port}");

            // استفاده از دستور switch برای شناسایی نوع پروتکل
            switch (protocolType.ToLower())
            {
                case "modbus":
                    return new ModbusProtocol(ip, port, loggerService);

                case "http":
                    var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                    return new Http_Protocol(httpClientFactory, $"http://{ip}:{port}", loggerService);

                case "signalar":
                    var hubUrl = _configuration.GetValue<string>("ProtocolSettings:SignalR:HubUrl") ?? "http://localhost:5000/signalhub";
                    return new SignalRProtocol(hubUrl, loggerService);

                default:
                    loggerService.LogError($"Unsupported protocol type: {protocolType}");
                    throw new ArgumentException($"❌ Unsupported protocol type: {protocolType}");
            }
        }
    }
}
